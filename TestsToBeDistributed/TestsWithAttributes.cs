using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestsToBeDistributed
{
    [TestClass]
    public class TestsWithAttributes
    {
        [TestMethod]
        [TestCategory("perfOnly")]
        [TestCategory("APHardware")]
        public void Performance_test_one()
        {
            var sleepTime = new Random(DateTime.Now.Millisecond*DateTime.Now.DayOfYear);
            Thread.Sleep(sleepTime.Next(10000));
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Performance_test_two()
        {
            Performance_test_one();   
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Performance_test_three()
        {
            Performance_test_one();
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Performance_test_four()
        {
            Performance_test_one();
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Performance_test_five()
        {
            Performance_test_one();
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Performance_test_six()
        {
            Performance_test_one();
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Performance_test_seven()
        {
            Performance_test_one();
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Performance_test_eight()
        {
            Performance_test_one();
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Performance_test_nine()
        {
            Performance_test_one();
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Performance_test_ten()
        {
            Performance_test_one();
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Failing_performance_test_one()
        {
            Performance_test_one();
            Assert.Fail("deliberate failure");
        }

        [TestMethod]
        [TestCategory("perfOnly")]
        public void Failing_performance_test_two()
        {
            Performance_test_one();
            Assert.Fail("deliberate failure");
        }
        [TestMethod]
        [TestCategory("perfOnly")]
        public void Failing_performance_test_three()
        {
            Performance_test_one();
            Assert.Fail("deliberate failure");
        }
        [TestMethod]
        [TestCategory("perfOnly")]
        public void Failing_performance_test_four()
        {
            Performance_test_one();
            Assert.Fail("deliberate failure");
        }
        [TestMethod]
        [TestCategory("perfOnly")]
        public void Failing_performance_test_five()
        {
            Performance_test_one();
            Assert.Fail("deliberate failure");
        }

        [TestMethod]
        [TestCategory("perfOnlyWithTypo")]
        public void This_test_should_never_be_run()
        {
            Performance_test_one();
            Assert.Fail("deliberate failure");
        }

    }
}
