using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace TestRunAgent
{
    class Program
    {
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
                var testName = resultInfo.testName;
                var testResultUri = new Uri(resultInfo.resultUri.ToString());

                //Post a test result
                using (var multipartFormDataContent = new MultipartFormDataContent())
                {
                    var trx = new XDocument(
                        new XComment("This is a comment"),
                        new XElement("Root",
                            new XElement("Child1", "data1"),
                            new XElement("Child2", "data2"),
                            new XElement("Child3", "data3"),
                            new XElement("Child2", "data4"),
                            new XElement("Info5", "info5"),
                            new XElement("Info6", "info6"),
                            new XElement("Info7", "info7"),
                            new XElement("Info8", "info8")
                        )
                    );

                    var encodedContent = Encoding.UTF8.GetBytes(trx.ToString());

                    multipartFormDataContent.Add(new ByteArrayContent(encodedContent), "File");
                    result = client.PostAsync(testResultUri, multipartFormDataContent).Result;
                    Console.ReadLine();
                }
            }
        }
    }
}
