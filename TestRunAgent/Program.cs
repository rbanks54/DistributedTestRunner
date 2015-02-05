using System;
using System.Collections.Generic;
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
            var baseUri = new Uri("http://rb-w230st:6028/");

            //Fugly loop code. Needs refactoring, but you should get the idea
            Console.WriteLine("hit enter after each result to load and run the next test");
            while (true)
            {
                //ask for a new test run
                using (var client = new HttpClient())
                {
                    //Get a test to execute (generate machine name based on current second)
                    var requestUri = new Uri(baseUri, "NextTest?machineName=" + DateTime.Now.Second);
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
                    if (mstest.ExitCode != 0)
                    {
                        Console.WriteLine("failed test");
                    }

                    //Post a test result
                    using (var multipartFormDataContent = new MultipartFormDataContent())
                    {
                        //We need to read the test result file and send it to the controller
                        var trx = XDocument.Load(resultFilePath);

                        var encodedContent = Encoding.UTF8.GetBytes(trx.ToString());

                        multipartFormDataContent.Add(new ByteArrayContent(encodedContent), "File");
                        result = client.PostAsync(testResultUri, multipartFormDataContent).Result;
                    }
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + ": test completed");
                }
                var key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Enter)
                    break;
            }
            Console.WriteLine("hit anything to exit");
            Console.ReadKey();
        }
    }
}
