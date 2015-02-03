using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Xml.Linq;

namespace TestRunController
{
    public class TestRunController : ApiController
    {
        [Route("nextTest")]
        public HttpResponseMessage Get(string machineName)
        {
            return Get("",machineName);
        }

        [Route("nextTest/{categoryName}")]
        public HttpResponseMessage Get(string categoryName, string machineName)
        {
            var currentTestRun = CommandController.TestRuns.FirstOrDefault(t => t.RunStatus == RunStatus.Started);
            if (currentTestRun == null)
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            var testName = currentTestRun.NextTest(categoryName,machineName);
            if (string.IsNullOrEmpty(testName))
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            var x = new {
                testName = testName, 
                resultUri = new Uri(this.Request.RequestUri, string.Format("testRun/{0}/testResult/{1}?machineName={2}", currentTestRun.Id, testName, machineName)).ToString()
            };
            var response = Request.CreateResponse(HttpStatusCode.OK, x);
            return response;
        }

        [Route("testRun/{id}/testResult/{testName}")]
        [HttpPost]
        public async Task<HttpResponseMessage> Put(string id, string testName, string machineName)
        {
            Guid testRunGuid;
            if (!Guid.TryParse(id, out testRunGuid) || 
                string.IsNullOrEmpty(machineName) || 
                string.IsNullOrEmpty(testName))
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
            
            var testRun = CommandController.TestRuns.FirstOrDefault(tr => tr.Id.Equals(testRunGuid));
            if (testRun == null)
                return new HttpResponseMessage(HttpStatusCode.BadRequest);

            if (testRun.RunStatus != RunStatus.Started)
                throw new HttpResponseException(HttpStatusCode.BadRequest);

            if (Request.Content.IsMimeMultipartContent())
            {
                var streamProvider = new MultipartMemoryStreamProvider();

                await Request.Content.ReadAsMultipartAsync(streamProvider);

                if (streamProvider.Contents.Count > 1) //only upload one trx at a time
                    throw new HttpResponseException(HttpStatusCode.BadRequest);

                foreach (var file in streamProvider.Contents)
                {
                    var content = file.ReadAsStringAsync().Result;
                    var trx = XDocument.Parse(content);
                    try
                    {
                        testRun.AddTestResult(testName, machineName, trx);
                    }
                    catch
                    {
                        throw new HttpResponseException(HttpStatusCode.InternalServerError);
                    }
                    }

                return new HttpResponseMessage(HttpStatusCode.Created);
            }
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.BadRequest, "Need to upload results as mimemultipartcontent");
            throw new HttpResponseException(response);
        }
    }
}
