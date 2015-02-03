using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Serialization;

namespace TestRunController
{
    public class CommandController: ApiController
    {
        //Commands to start/stop/restart the entire test controller
        //Test runs are created with the testrun api

        //Yay for statics! We need to make sure this thing, and everything it contains, is thread safe
        internal static readonly ConcurrentBag<TestRun> TestRuns = new ConcurrentBag<TestRun>(); 

        public HttpResponseMessage Post([FromBody]string command)
        {
            switch (command)
            {
                case "start":
                    return Start();
                    break;
                case "stop":
                    return Stop();
                    break;
                case "restart":
                    return Start(); //restart and start do the same thing internally.
                    break;
                default:
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        private HttpResponseMessage Start()
        {
            Stop();
            try
            {
                TestRuns.First(r => r.RunStatus == RunStatus.Waiting).Start();
            }
            catch (InvalidOperationException)
            {
                //no test runs waiting to start? No problem. We'll just move on.
                //any other exceptions thrown by .Start() can bubble up the stack.
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private HttpResponseMessage Stop()
        {
            foreach (
                var run in TestRuns.Where(r => r.RunStatus == RunStatus.Started))
            {
                run.Stop();
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [Route("testRun")]
        [HttpPost]
        public HttpResponseMessage NewTestRun([FromBody] string testDll)
        {
            var tests = AssemblyScanner.ScanDll(testDll);
            var testRun = new TestRun();
            foreach (var test in tests)
            {
                testRun.AddTestToQueues(test);
            }
            TestRuns.Add(testRun);
            var response = new HttpResponseMessage(HttpStatusCode.Created);
            response.Headers.Location = new Uri(this.Request.RequestUri,"/testRun/" + testRun.Id );
            return response;
        }
    }
}
