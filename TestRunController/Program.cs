using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace TestRunController
{
    class Program
    {
        public static System.Threading.ManualResetEvent shutDown = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            string baseAddress = ConfigurationManager.AppSettings.Get("uriReservation");
            if (string.IsNullOrEmpty(baseAddress))
            {
                baseAddress = "http://+:6028/";
            }

            // Start OWIN host 
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Console.WriteLine(@"Waiting for a ""shutdown"" command");
                shutDown.WaitOne();
                Thread.Sleep(1000); //Allow time to send the response back to the client that requested the shutdown
            }
        }
    }
}
