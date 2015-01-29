using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestsToBeDistributed
{
    [TestClass]
    public class TestsWithoutAttributes
    {
        [TestMethod]
        public void Regular_test_one()
        {
            var sleepTime = new Random(DateTime.Now.Millisecond * DateTime.Now.DayOfYear);
            Thread.Sleep(sleepTime.Next(20000));
        }

        [TestMethod]
        public void Regular_test_two()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Regular_test_three()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Regular_test_four()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Regular_test_five()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Regular_test_six()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Regular_test_seven()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Regular_test_eight()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Regular_test_nine()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Regular_test_ten()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Regular_test_eleven()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Regular_test_twelve()
        {
            Regular_test_one();
        }

        [TestMethod]
        public void Failing_regular_test_one()
        {
            Regular_test_one();
            Assert.Fail("ONOES! I'm broken!");
        }
        [TestMethod]
        public void Failing_regular_test_two()
        {
            Regular_test_one();
            Assert.Fail("ONOES! I'm broken!");
        }
        [TestMethod]
        public void Failing_regular_test_three()
        {
            Regular_test_one();
            Assert.Fail("ONOES! I'm broken!");
        }
    }
}
