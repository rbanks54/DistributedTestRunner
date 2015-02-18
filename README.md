# DistributedTestRunner
This is a proof of concept for a distributed test controller/agent. And is for idea exploration only.
It's not expected to be useful :-)

The basic idea is that there is a centralised test API that various 'agents' call to see what test they should be running next.
The agents post the test result back to the API and then magic needs to happen to determine if the test run has ended.

The code uses test categories, and an agent can request tests for a specific category.
Tests with categories that are unrequested won't be executed.
Tests in multiple categories will get executed twice (at this point in time).

For background information see the [original blog post](http://www.richard-banks.org/2015/02/side-project-distributed-test-runner.html) about it.

Feel free to use and borrow as you wish!
