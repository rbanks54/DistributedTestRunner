using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace TestRunController
{
    public class NextTestController : ApiController
    {
        [Route("nextTest")]
        public HttpResponseMessage Get(string machineName)
        {
            return Get("",machineName);
        }

        [Route("nextTest/{categoryName}")]
        public HttpResponseMessage Get(string category, string machineName)
        {
            var currentTestRun = CommandController.TestRuns.FirstOrDefault(t => t.RunStatus == RunStatus.Started);
            if (currentTestRun == null)
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            var testName = currentTestRun.NextTest(category,machineName);
            if (string.IsNullOrEmpty(testName))
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            var response = Request.CreateResponse(HttpStatusCode.OK, testName);
            return response;
        }
    }
}
