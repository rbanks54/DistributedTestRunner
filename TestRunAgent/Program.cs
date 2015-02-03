using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Policy;
using System.Text;
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

            //ask for a new test run
            var requestUri = new Uri(baseUri, "TestRun");
            using (var client = new HttpClient())
            {
                //Load the tests
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var request = new HttpRequestMessage(HttpMethod.Post, requestUri);

                request.Content = new StringContent("TestsToBeDistributed.dll",Encoding.UTF8,"text/plain");
                var result = client.SendAsync(request).Result;
                var testUri = result.Headers.Location;

                //Start the test run
                request = new HttpRequestMessage(HttpMethod.Post,testUri);
                request.Content = new StringContent("start");
                result = client.SendAsync(request).Result;
                
                //Get a test to execute
                requestUri = new Uri(baseUri, "NextTest?machineName=1");
                result = client.GetAsync(requestUri).Result;
                var testToExecute = result.Content.ReadAsStringAsync().Result;
                dynamic resultInfo = JsonConvert.DeserializeObject(testToExecute);
                var testName = resultInfo.testName.ToString();
                var testResultUri = new Uri(resultInfo.resultUri.ToString());



                var arguments = String.Format("/testContainer:TestsToBeDistributed.dll /test:{0} /resultsfile:testresult.trx /nologo", testName);
                var mstest = new Process();
                mstest.StartInfo = new ProcessStartInfo(MSTEST,arguments);
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
                    Console.ReadLine();
                }
            }
        }
    }
}
