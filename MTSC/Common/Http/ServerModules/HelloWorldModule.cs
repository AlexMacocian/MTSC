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
        bool IHttpModule.HandleRequest(HttpHandler handler, ClientData client, HttpMessage request, ref HttpMessage response)
        {
            if (request.Method == HttpMessage.MethodEnum.Get)
            {
                //client.ToBeRemoved = true;
                response.StatusCode = HttpMessage.StatusCodes.OK;
                response[HttpMessage.GeneralHeadersEnum.Date] = DateTime.Now.ToString();
                response[HttpMessage.EntityHeadersEnum.ContentType] = "text/plain; charset=UTF-8";
                response["Server"] = "MTSC";
                response.Body = this.response;
            }
            return true;
        }
    }
}
