using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace TestRunAgent
{
    internal class TrxMerge
    {
        internal static XDocument MergeResults(XDocument sourceResult, XDocument targetResults)
        {
            var testDefinitionXName = XName.Get("TestDefinitions", @"http://microsoft.com/schemas/VisualStudio/TeamTest/2010");
            var resultsXName = XName.Get("Results", @"http://microsoft.com/schemas/VisualStudio/TeamTest/2010");

            ////locate sections in first and append data from second...
            var targetDefinitions = targetResults.Descendants(testDefinitionXName).First();

            foreach (var sourceDefinition in sourceResult.Descendants(testDefinitionXName).First().Elements())
            {
                var sourceTestName = sourceDefinition.Attribute("name").Value;

                if (targetDefinitions.Elements().All(d => d.Attribute("name").Value != sourceTestName))
                {
                    targetDefinitions.Add(sourceDefinition);
                }
            }

            var targetTestResults = targetResults.Descendants(resultsXName).First();
            foreach (var sourceTestResult in sourceResult.Descendants(resultsXName).First().Elements())
            {
                var sourceTestName = sourceTestResult.Attribute("testName").Value;
                var targetTestResult =
                    targetTestResults.Elements().FirstOrDefault(d => d.Attribute("testName").Value == sourceTestName);

                if (targetTestResult == null)
                {
                    targetTestResults.Add(sourceTestResult);
                }
                else
                {
                    targetTestResult.ReplaceWith(sourceTestResult);
                }
            }
            return targetResults;
        }
    }
}

