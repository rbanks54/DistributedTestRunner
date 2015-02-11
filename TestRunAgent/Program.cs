using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace TestRunAgent
{
    class Program
    {
        private const string MSTEST = @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\mstest.EXE";

        static void Main(string[] args)
        {
            var baseUri = GetBaseUriFromConfiguration();
            var testCategory = GetTestCategoryFromConfiguration();

            XDocument trxResults = null;

            //Fugly loop code. Needs refactoring, but you should get the idea
            Console.WriteLine("hit enter after each result to load and run the next test");
            while (true)
            {
                //ask for a new test run
                using (var client = new HttpClient())
                {
                    Uri requestUri;
                    //Get a test to execute (generate machine name based on current second)
                    if (string.IsNullOrEmpty(testCategory))
                    {
                        requestUri = new Uri(baseUri, "NextTest?machineName=" + DateTime.Now.Second);
                    }
                    else
                    {
                        requestUri = new Uri(baseUri,string.Format("NextTest/{0}?machineName={1}",testCategory,DateTime.Now.Second));
                    }
                    var result = client.GetAsync(requestUri).Result;
                    if (result.StatusCode == HttpStatusCode.NoContent)
                    {
                        Console.WriteLine(DateTime.Now.ToShortTimeString() + ": Waiting for test");
                        Thread.Sleep(1000);
                        continue;
                    }
                    var testToExecute = result.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + testToExecute);
                    dynamic resultInfo = JsonConvert.DeserializeObject(testToExecute);
                    var testName = resultInfo.testName.ToString();
                    var testResultUri = new Uri(resultInfo.resultUri.ToString());

                    var arguments =
                        String.Format(
                            "/testContainer:TestsToBeDistributed.dll /test:{0} /resultsfile:testresult.trx /nologo",
                            testName);
                    var mstest = new Process();
                    mstest.StartInfo = new ProcessStartInfo(MSTEST, arguments);
                    mstest.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    mstest.StartInfo.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    mstest.StartInfo.UseShellExecute = false;
                    mstest.StartInfo.RedirectStandardError = true;
                    mstest.StartInfo.RedirectStandardOutput = true;

                    //ensure the result file is deleted before starting MSTest
                    var resultFilePath = Path.Combine(mstest.StartInfo.WorkingDirectory, "testresult.trx");
                    if (File.Exists(resultFilePath))
                        File.Delete(resultFilePath);

                    mstest.Start();
                    var errors = mstest.StandardError.ReadToEnd();
                    var output = mstest.StandardOutput.ReadToEnd();
                    mstest.WaitForExit();
                    var success = (mstest.ExitCode != 0);

                    //Read and merge the mstest results
                    var latestResults = XDocument.Load(resultFilePath);
                    trxResults = trxResults == null ? latestResults : TrxMerge.MergeResults(latestResults, trxResults);
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + ": test completed");

                    //Tell the controller that this test is completed
                    result = client.PutAsync(testResultUri, new StringContent(success.ToString())).Result;
                }
                var key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Enter)
                    break;
            }
            Console.WriteLine("hit anything to exit");

            //Need to dump out the merged results to a file. Base it on the testRunId
            trxResults.Save(string.Format("testResults_{0}.trx", DateTime.Now.ToString("yyyyMMdd")));

            //using (var multipartFormDataContent = new MultipartFormDataContent())
            //{
            //    //We need to read the test result file and send it to the controller
            //    var trx = XDocument.Load(resultFilePath);

            //    var encodedContent = Encoding.UTF8.GetBytes(trx.ToString());

            //    multipartFormDataContent.Add(new ByteArrayContent(encodedContent), "File");
            //    result = client.PostAsync(testResultUri, multipartFormDataContent).Result;
            //}
            Console.ReadKey();
        }

        private static Uri GetBaseUriFromConfiguration()
        {
            var baseAddress = ConfigurationManager.AppSettings.Get("controllerBaseAddress");
            if (string.IsNullOrEmpty(baseAddress))
            {
                baseAddress = "http://localhost:6028/";
            }
            if (!baseAddress.EndsWith("/"))
            {
                baseAddress = string.Concat(baseAddress, "/");
            }
            var baseUri = new Uri(baseAddress);
            return baseUri;
        }
        private static string GetTestCategoryFromConfiguration()
        {
            var category = ConfigurationManager.AppSettings.Get("testCategory");
            if (string.IsNullOrEmpty(category))
            {
                category = string.Empty;
            }
            return category;
        }
    }
}
