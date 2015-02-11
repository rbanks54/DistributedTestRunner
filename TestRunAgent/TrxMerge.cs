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

    //    internal static Summary GetSummary(XmlDocument doc)
    //    {
    //        Summary s = new Summary();
    //        s.Total = -1;
    //        s.Executed = -1;
    //        s.Passed = -1;

    //        //XmlElement ele = doc.DocumentElement;

    //        XmlNode nTotal = doc.SelectNodes("//Counters/@total").Item(0);
    //        s.Total = Convert.ToInt32(nTotal.InnerText);
    //        XmlNode nPass = doc.SelectNodes("//Counters/@passed").Item(0);
    //        s.Passed = Convert.ToInt32(nPass.InnerText);
    //        XmlNode nExecuted = doc.SelectNodes("//Counters/@executed").Item(0);
    //        s.Executed = Convert.ToInt32(nExecuted.InnerText);
    //        DateTime start;
    //        DateTime end0;
    //        XmlNode nStart = doc.SelectNodes("//Times/@start ").Item(0);
    //        start = Convert.ToDateTime(nStart.InnerText);
    //        XmlNode nEnd = doc.SelectNodes("//Times/@finish").Item(0);
    //        end0 = Convert.ToDateTime(nEnd.InnerText);
    //        //s.time = Math.Round((double)(end0 - start)/10000000.0,2); //ticks are in 100-nanoseconds
    //        s.Time = ((end0 - start).TotalSeconds);

    //        return s;
    //    }

    //    public static string MakeCompatXML(string input)
    //    {
    //        //change the first tag to have no attributes - VSTS2008
    //        StringBuilder newFile = new StringBuilder();
    //        string temp = "";
    //        string nStr = "";
    //        string[] file = File.ReadAllLines(input);

    //        foreach (string line in file)
    //        {
    //            if (line.Contains("<TestRun id"))
    //            {
    //                nStr = line.Substring(0, 8) + ">";
    //                temp = line.Replace(line.ToString(), nStr);
    //                newFile.Append(temp + "\r");
    //                continue;
    //            }

    //            newFile.Append(line + "\r");
    //        }

    //        File.WriteAllText(input, newFile.ToString());

    //        return input;
    //    }

    //    public static void SetSummary(string fileName)
    //    {
    //        System.Xml.XmlDocument oDoc = new XmlDocument();
    //        oDoc.Load(fileName);

    //        XmlNode master = oDoc.SelectSingleNode("//ResultSummary/Counters");

    //        Summary oSummary;
    //        oSummary.Passed = 0;

    //        //count the number of test cases for total
    //        oSummary.Total = oDoc.SelectSingleNode("//TestDefinitions").ChildNodes.Count;

    //        //count the number of test cases executed from count of test results
    //        oSummary.Executed = oDoc.SelectSingleNode("//Results").ChildNodes.Count;

    //        //count the number of passed test cases from results
    //        int i = 0;
    //        while (oDoc.SelectSingleNode("//Results").ChildNodes.Count != i)
    //        {
    //            if (oDoc.SelectSingleNode("//Results").ChildNodes[i].Attributes["outcome"].Value == "Passed")
    //            {
    //                oSummary.Passed++;
    //            }
    //            i++;
    //        }

    //        ////update summary with new numbers
    //        master.Attributes["total"].Value = oSummary.Total.ToString();
    //        master.Attributes["executed"].Value = oSummary.Executed.ToString();
    //        master.Attributes["passed"].Value = oSummary.Passed.ToString();

    //        ////locate and update times
    //        XmlNode oTimes = oDoc.SelectSingleNode("//Times");
    //        //add a new attribute elapsed time, original trx would not have that
    //        XmlAttribute elapsed = oDoc.CreateAttribute("elapsedtime");
    //        elapsed.Value = CalculateSummaryTime(oDoc).ToString();
    //        oTimes.Attributes.Append(elapsed);

    //        oDoc.Save(fileName);

    //    }

    //    public static double CalculateSummaryTime(XmlDocument doc)
    //    {
    //        DateTime sTime;
    //        DateTime eTime;
    //        double sDiff = 0;

    //        int i = 0;

    //        while (doc.SelectSingleNode("//Results").ChildNodes.Count != i)
    //        {
    //            sTime = Convert.ToDateTime(doc.SelectSingleNode("//Results").ChildNodes[i].Attributes["startTime"].Value);
    //            eTime = Convert.ToDateTime(doc.SelectSingleNode("//Results").ChildNodes[i].Attributes["endTime"].Value);
    //            //// calculate timespan for each test case and add them to calculate total time
    //            sDiff += (eTime - sTime).TotalSeconds;
    //            i++;
    //        }
    //        return sDiff;
    //    }
    //}

    //struct Summary
    //{
    //    public int Total;
    //    public int Executed;
    //    public int Passed;
    //    public double Time;
    //}
}

