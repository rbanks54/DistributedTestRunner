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

        //The intent here is to start/stop the entire controller (i.e. prevent new test runs, etc)
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

        [Route("status")]
        [HttpGet]
        public HttpResponseMessage Status()
        {
            HttpResponseMessage response;
            var currentRun = TestRuns.FirstOrDefault(r => r.RunStatus == RunStatus.Started);
            if (currentRun == null)
            {
                var result = new
                {
                    isActive = false,
                };
                response = Request.CreateResponse(result);
            }
            else
            {
                var result = new
                {
                    isActive = true,
                    runId = currentRun.Id.ToString(),
                    queuedTests = currentRun.RemainingTests,
                    completedTests = currentRun.CompletedTests,
                    inProgressTests = currentRun.InProgressTests,
                    inProgressTestNames = currentRun.ActiveTestNames,
                };
                response = Request.CreateResponse(result);
            }
            return response;
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
        public HttpResponseMessage NewTestRun()
        {
            var testDll = Request.Content.ReadAsStringAsync().Result;
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

        [Route("testRun/{id}")]
        [HttpPost]
        public HttpResponseMessage TestRunCommands(string id)
        {
            var command = Request.Content.ReadAsStringAsync().Result;
            Guid testId;
            if (!Guid.TryParse(id, out testId))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            var testRun = TestRuns.FirstOrDefault(r => r.Id.Equals(testId));
            if (testRun == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            switch (command.ToLowerInvariant())
            {
                case "start":
                    if (testRun.RunStatus == RunStatus.Waiting)
                    {
                        testRun.Start();
                    }
                    //if the testrun isn't waiting there no action to be taken (we don't restart test runs)
                    return new HttpResponseMessage(HttpStatusCode.OK);
                    break;
                case "stop":
                    testRun.Stop();
                    return new HttpResponseMessage(HttpStatusCode.OK);
                    break;
                default:
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

    }
}
