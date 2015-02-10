using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin.Hosting;

namespace TestRunController
{
    class Program
    {
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
                Console.WriteLine("Hit enter to shut this puppy down");
                Console.ReadLine();
            }
        }
    }
}
