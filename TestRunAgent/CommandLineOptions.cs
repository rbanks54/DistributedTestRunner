using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace TestRunAgent
{
    public class CommandLineOptions
    {
        [Option('c', "controller", Required = false, HelpText = "The base address of the controller (include the 'http://')")]
        public string ServerUri { get; set; }

        // omitting long name, default --verbose
        [Option('t', "testCategory", Required = false, HelpText = "Specific test category of tests to run")]
        public string TestCategory { get; set; }

        [Option('m', "machineId", Required = false, HelpText = "Enter the machine id for this test agent")]
        public string MachineId { get; set; }
    }
}
