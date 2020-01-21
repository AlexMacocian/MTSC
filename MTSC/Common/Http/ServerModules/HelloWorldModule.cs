using System;
using System.Collections.Generic;
using System.Text;
using MTSC.Server;
using MTSC.Server.Handlers;

namespace MTSC.Common.Http.ServerModules
{
    public class HelloWorldModule : IHttpModule
    {
        byte[] response = ASCIIEncoding.ASCII.GetBytes("Hello, World!");
        bool IHttpModule.HandleRequest(Server.Server server, HttpHandler handler, ClientData client, HttpRequest request, ref HttpResponse response)
        {
            if (request.Method == HttpMessage.HttpMethods.Get)
            {
                //client.ToBeRemoved = true;
                response.StatusCode = HttpMessage.StatusCodes.OK;
                response.Headers[HttpMessage.GeneralHeaders.Date] = DateTime.Now.ToString();
                response.Headers[HttpMessage.EntityHeaders.ContentType] = "text/plain; charset=UTF-8";
                response.Headers["Server"] = "MTSC";
                response.Body = this.response;
            }
            return true;
        }

        void IHttpModule.Tick(Server.Server server, HttpHandler handler)
        {
            
        }
    }
}
