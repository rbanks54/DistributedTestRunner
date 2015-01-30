using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

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
            Id = new Guid();
            testQueues = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
            RunStatus = RunStatus.Waiting;
        }

        public Guid Id { get; set; }
        public RunStatus RunStatus { get; private set; }

        public void Start()
        {
            RunStatus = RunStatus.Started;
        }

        public void Stop()
        {
            //Only set to aborted if there's work remaining or in progress
            if (Interlocked.Read(ref remainingTests) + Interlocked.Read(ref inProgressTests) > 0)
            {
                RunStatus = RunStatus.Aborted;
            }
            //Finished status is set when the final test returns a result, no need to set it here
        }

        public void AddTestToQueues(TestMetaData testToAdd)
        {
            if (testToAdd.TestAttributes == null || !testToAdd.TestAttributes.Any())
            {
                AddTestToQueue(string.Empty,testToAdd.TestName);
            }
            else
            {
                foreach (var attribute in testToAdd.TestAttributes)
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
                Interlocked.Increment(ref remainingTests);
            }
            var queue = testQueues[queueName];
            queue.Enqueue(testName);
        }

        public string NextTest(string machineName)
        {
            ConcurrentQueue<string> queue;
            if (!testQueues.TryGetValue(string.Empty, out queue)) return string.Empty;

            return NextTestFromQueue(machineName, queue);
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
                return testName;
            }
            return string.Empty;
        }

        public string NextTest(string attributeName, string machineName)
        {
            ConcurrentQueue<string> queue;
            if (!testQueues.TryGetValue(attributeName, out queue)) return string.Empty;

            return NextTestFromQueue(machineName, queue);
        }
    }

    public class TestMetaData
    {
        public string TestName { get; set; }
        public IEnumerable<string> TestAttributes { get; set; }
    }

    public class TestResult
    {
        public string TestName { get; set; }
        public XDocument TestResultXml { get; set; }
    }

    public enum RunStatus
    {
        Waiting,
        Started,
        Completed,
        Aborted
    }
}
