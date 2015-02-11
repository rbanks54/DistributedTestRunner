using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Deployment.Internal;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Hosting;
using System.Xml.Linq;
using Timer = System.Timers.Timer;

namespace TestRunController
{
    public class TestRun
    {
        private ConcurrentDictionary<string, ConcurrentQueue<string>> testQueues;
        private ConcurrentDictionary<string, string> activeTests; 
        private ConcurrentBag<TestResult> testResults;
        private long remainingTests = 0;
        private long inProgressTests = 0;
        private long completedTests = 0;

        public TestRun()
        {
            Id = Guid.NewGuid();
            testQueues = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
            activeTests= new ConcurrentDictionary<string, string>();
            testResults = new ConcurrentBag<TestResult>();
            RunStatus = RunStatus.Waiting;

            int timeoutInSeconds;
            int.TryParse(ConfigurationManager.AppSettings.Get("testRunTimeout"), out timeoutInSeconds);
            TestRunIdleTimer = new Timer(timeoutInSeconds * 1000);
        }

        public Guid Id { get; private set; }
        public RunStatus RunStatus { get; private set; }
        
        /// <summary>
        /// Length of time before we decide no more tests are being requested (in seconds)
        /// </summary>
        public int QueueTimeout { get; set; }

        public long RemainingTests
        {
            get { return remainingTests; }
        }

        public long InProgressTests
        {
            get { return inProgressTests; }
        }

        public long CompletedTests
        {
            get { return completedTests; }
        }

        public IEnumerable<TestRunningOnMachine> ActiveTestNames
        {
            get { return activeTests.Select(t => new TestRunningOnMachine(){TestName = t.Value, MachineName = t.Key}).AsEnumerable(); }
        }

        public Timer TestRunIdleTimer { get; private set; }

        public void Start()
        {
            RunStatus = RunStatus.Started;
            TestRunIdleTimer.Start();
        }

        public void Stop()
        {
            //Only set to aborted if there's work remaining or in progress
            if (Interlocked.Read(ref remainingTests) + Interlocked.Read(ref inProgressTests) > 0)
            {
                RunStatus = RunStatus.Aborted;
                CommandController.StartNextTestRun();
            }
            TestRunIdleTimer.Stop();
            //Finished status is set when the final test returns a result, no need to set it here
        }

        public void AddTestToQueues(TestMetaData testToAdd)
        {
            var attributes = testToAdd.TestAttributes();
            if (attributes == null || !attributes.Any())
            {
                AddTestToQueue(string.Empty,testToAdd.TestName);
            }
            else
            {
                foreach (var attribute in attributes)
                {
                    AddTestToQueue(attribute,testToAdd.TestName);
                }
            }
        }

        private void AddTestToQueue(string queueName, string testName)
        {
            if (RunStatus != RunStatus.Waiting) return;

            if (!testQueues.ContainsKey(queueName))
            {
                testQueues.TryAdd(queueName, new ConcurrentQueue<string>());
            }
            var queue = testQueues[queueName];
            queue.Enqueue(testName);
            Interlocked.Increment(ref remainingTests);
        }

        private string NextTestFromQueue(string machineName, ConcurrentQueue<string> queue)
        {
            if (RunStatus != RunStatus.Started) return string.Empty;

            string testName;
            if (queue.TryDequeue(out testName))
            {   
                Interlocked.Decrement(ref remainingTests);
                Interlocked.Increment(ref inProgressTests);
                activeTests.TryAdd(machineName, testName);
                TestRunIdleTimer.Stop(); //a machine requested another test (and we have one to send it)
                return testName;
            }
            return string.Empty;
        }

        public string NextTest(string attributeName, string machineName)
        {
            //Return current test for the machine if one is in progress
            string currentTest;
            if (activeTests.TryGetValue(machineName, out currentTest))
            {
                if (!string.IsNullOrEmpty(currentTest))
                {
                    return currentTest;
                }
            }

            //otherwise we grab a test from a queue
            ConcurrentQueue<string> queue;
            if (!testQueues.TryGetValue(attributeName, out queue)) return string.Empty;

            return NextTestFromQueue(machineName, queue);
        }

        public void AddTestResult(string testName, string machineName, bool success) //, XDocument trxDocument)
        {
            if (string.IsNullOrEmpty(machineName) || testResults.Any(t => t.TestName == testName))
                throw new ApplicationException("Test result already uploaded or machine name not set");

            var tr = new TestResult() {TestName = testName, Success = success}; //  TestResultXml = trxDocument};
            testResults.Add(tr);

            Interlocked.Decrement(ref inProgressTests);
            Interlocked.Increment(ref completedTests);

            string ignored;
            activeTests.TryRemove(machineName, out ignored);

            if (InProgressTests == 0 && RemainingTests == 0)
            {
                RunStatus = RunStatus.Completed;
                CommandController.StartNextTestRun();
            }
            else
            {
                TestRunIdleTimer.Start(); //Start the idle timer. It will be reset when a machine next requests a test
            }
        }
    }

    public class TestMetaData
    {
        private List<string> testAttributes = new List<string>();  
        public TestMetaData()
        {
            testAttributes = new List<string>();
        }

        public string TestName { get; set; }
        public IQueryable<string> TestAttributes()
        {
            return testAttributes.AsQueryable();
        }

        public void AddAttribute(string attributeName)
        {
            testAttributes.Add(attributeName);
        }
    }

    public class TestResult
    {
        public string TestName { get; set; }
        public bool Success { get; set; }
        public XDocument TestResultXml { get; set; }
    }

    public class TestRunningOnMachine
    {
        public string MachineName { get; set; }
        public string TestName { get; set; }
    }

    public enum RunStatus
    {
        Waiting,
        Started,
        Completed,
        Aborted
    }
}
