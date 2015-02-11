﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommandLine.Text;
using Newtonsoft.Json;

namespace TestRunAgent
{
    class Program
    {
        private const string MSTEST = @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\mstest.EXE";

        static void Main(string[] args)
        {
            var options = new CommandLineOptions();
            var parseResult = CommandLine.Parser.Default.ParseArgumentsStrict(args,options);
            if (!parseResult)
            {
                Console.WriteLine("Invalid options. Please try again");
                Console.WriteLine(HelpText.AutoBuild(options));
                return;
            }
            
            var baseUri = GetBaseUriFromConfiguration();
            if (options.ServerUri != null)
            {
                baseUri = new Uri(options.ServerUri);
            }
            var testCategory = options.TestCategory ?? GetValueFromConfiguration("testCategory");
            var machineId = options.MachineId ?? GetValueFromConfiguration("machineId");

            XDocument trxResults = null;
            var testRunInProgress = false;

            Console.WriteLine("hit enter after each result to load and run the next test");
            while (true)
            {
                //ask for a new test run
                using (var client = new HttpClient())
                {
                    var result = RequestNextTest(testCategory, baseUri, machineId, client);
                    if (result == null || result.StatusCode == HttpStatusCode.NoContent)
                    {
                        if (testRunInProgress)
                        {
                            trxResults.Save(string.Format("testResults_{1}_{0}.trx", machineId, DateTime.Now.ToString("yyyyMMdd")));
                            if (!options.StayAlive)
                                break;
                        }
                        testRunInProgress = false;
                        Console.WriteLine(DateTime.Now.ToShortTimeString() + ": Waiting for test");
                        Thread.Sleep(1000);
                        continue;
                    }
                    testRunInProgress = true;
                    var testToExecute = result.Content.ReadAsStringAsync().Result;
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + testToExecute);
                    dynamic resultInfo = JsonConvert.DeserializeObject(testToExecute);
                    var testName = resultInfo.testName.ToString();
                    var testResultUri = new Uri(resultInfo.resultUri.ToString());

                    string resultFilePath;
                    var success = RunMsTest(machineId, testName, out resultFilePath);

                    trxResults = AppendTestResultsAndCleanup(resultFilePath, trxResults);

                    //Tell the controller that this test is completed
                    result = client.PutAsync(testResultUri, new StringContent(success.ToString())).Result;
                }
            }

            //using (var multipartFormDataContent = new MultipartFormDataContent())
            //{
            //    //We need to read the test result file and send it to the controller
            //    var trx = XDocument.Load(resultFilePath);

            //    var encodedContent = Encoding.UTF8.GetBytes(trx.ToString());

            //    multipartFormDataContent.Add(new ByteArrayContent(encodedContent), "File");
            //    result = client.PostAsync(testResultUri, multipartFormDataContent).Result;
            //}
        }

        private static XDocument AppendTestResultsAndCleanup(string resultFilePath, XDocument trxResults)
        {
            var latestResults = XDocument.Load(resultFilePath);
            trxResults = trxResults == null ? latestResults : TrxMerge.MergeResults(latestResults, trxResults);
            Console.WriteLine(DateTime.Now.ToShortTimeString() + ": test completed");

            //Optional - delete the run deployment. Feel free to not do this.
            var runDeploymentRoot =
                latestResults.Descendants(XName.Get("Deployment", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"))
                    .First().Attribute("runDeploymentRoot").Value;
            Directory.Delete(runDeploymentRoot, true);
            return trxResults;
        }

        private static bool RunMsTest(string machineId, dynamic testName, out string resultFilePath)
        {
            var testResultFileName = "testResult_" + machineId + ".trx";
            var arguments =
                String.Format(
                    "/testContainer:TestsToBeDistributed.dll /test:{0} /resultsfile:{1} /nologo",
                    testName, testResultFileName);
            var mstest = new Process();
            mstest.StartInfo = new ProcessStartInfo(MSTEST, arguments);
            mstest.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            mstest.StartInfo.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            mstest.StartInfo.UseShellExecute = false;
            mstest.StartInfo.RedirectStandardError = true;
            mstest.StartInfo.RedirectStandardOutput = true;

            //ensure the result file is deleted before starting MSTest
            resultFilePath = Path.Combine(mstest.StartInfo.WorkingDirectory, testResultFileName);
            if (File.Exists(resultFilePath))
                File.Delete(resultFilePath);

            mstest.Start();
            var errors = mstest.StandardError.ReadToEnd();
            var output = mstest.StandardOutput.ReadToEnd();
            mstest.WaitForExit();
            var success = (mstest.ExitCode != 0);
            return success;
        }

        private static HttpResponseMessage RequestNextTest(string testCategory, Uri baseUri, string machineId, HttpClient client)
        {
            Uri requestUri;
            //Get a test to execute (generate machine name based on current second)
            if (string.IsNullOrEmpty(testCategory))
            {
                requestUri = new Uri(baseUri, "NextTest?machineName=" + machineId);
            }
            else
            {
                requestUri = new Uri(baseUri, string.Format("NextTest/{0}?machineName={1}", testCategory, machineId));
            }
            try
            {
                var result = client.GetAsync(requestUri).Result;
                return result;
            }
            catch (Exception ex)
            {
                //The server address is wrong or the server isn't up. We'll continue waiting on the assumption that
                //the server address is correct, and just not running at the moment
                return null;
            }
        }

        private static Uri GetBaseUriFromConfiguration()
        {
            var baseAddress = GetValueFromConfiguration("controllerBaseAddress");
            if (baseAddress == string.Empty)
            {
                baseAddress = "http://localhost:6028/";
            }
            if (!baseAddress.EndsWith("/"))
            {
                baseAddress = string.Concat(baseAddress, "/");
            }
            var baseUri = new Uri(baseAddress);
            return baseUri;
        }

        private static string GetValueFromConfiguration(string keyName)
        {
            var value = ConfigurationManager.AppSettings.Get(keyName) ?? string.Empty;
            return value;
        }
    }
}
