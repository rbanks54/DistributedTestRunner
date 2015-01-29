using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Serialization;

namespace TestRunController
{
    public class CommandController: ApiController
    {
        public HttpResponseMessage Post([FromBody]string command)
        {
            switch (command)
            {
                case "start":
                    return Start();
                    break;
                case "stop":
                    return Stop();
                    break;
                case "restart":
                    return Start(); //restart and start do the same thing internally.
                    break;
                default:
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        private HttpResponseMessage Start()
        {
            Stop();
            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        private HttpResponseMessage Stop()
        {

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
