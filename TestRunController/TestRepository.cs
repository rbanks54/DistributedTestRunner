using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRunController
{
    public class TestRepository
    {
        //Not 100% thread safe yet - I need to put locks around the accessing of testQueues
        //as a reset command will replace the testQueues object.
        //I'll clean this up later.

        public TestRepository()
        {
            testQueues = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        }

        private ConcurrentDictionary<string,ConcurrentQueue<string>> testQueues;

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
            if (!testQueues.ContainsKey(queueName))
            {
                testQueues.TryAdd(queueName, new ConcurrentQueue<string>());
            }
            var queue = testQueues[queueName];
            queue.Enqueue(testName);
        }

        public string NextTest()
        {
            ConcurrentQueue<string> queue;
            if (!testQueues.TryGetValue(string.Empty, out queue)) return string.Empty;

            string testName;
            return queue.TryDequeue(out testName) ? testName : string.Empty;
        }

        public string NextTest(string attributeName)
        {
            ConcurrentQueue<string> queue;
            if (!testQueues.TryGetValue(attributeName, out queue)) return string.Empty;

            string testName;
            return queue.TryDequeue(out testName) ? testName : string.Empty;
        }

        public void EmptyAllQueues()
        {
            testQueues = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        }
    }

    public class TestMetaData
    {
        public string TestName { get; set; }
        public IEnumerable<string> TestAttributes { get; set; }
    }
}
