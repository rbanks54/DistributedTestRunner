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

        //The intent here is to send commands to the test controller
        //The only valuable one at the moment is the 'stop' command that just stops all active test runs.
        public HttpResponseMessage Post([FromBody]string command)
        {
            switch (command)
            {
                case "stop":
                    return Stop();
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
            StartNextTestRun();
            var response = new HttpResponseMessage(HttpStatusCode.Created);
            response.Headers.Location = new Uri(this.Request.RequestUri,"/testRun/" + testRun.Id );
            return response;
        }

        internal static void StartNextTestRun()
        {
            if (TestRuns.All(t => t.RunStatus != RunStatus.Started))
            {
                var testRun = TestRuns.FirstOrDefault(t => t.RunStatus == RunStatus.Waiting);
                if (testRun != null)
                {
                    testRun.Start();
                    testRun.TestRunIdleTimer.Elapsed += TestRunIdleTimer_Elapsed;
                }
            }
        }

        static void TestRunIdleTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Determine current test run and then stop it
            var testRun = TestRuns.First(t => t.RunStatus == RunStatus.Started);
            testRun.TestRunIdleTimer.Elapsed -= TestRunIdleTimer_Elapsed; //unhook the event listener
            testRun.Stop();
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
