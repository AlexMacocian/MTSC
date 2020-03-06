using System;
using System.Text;
using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.Common.Http.ServerModules
{
    public sealed class HelloWorldModule : IHttpModule
    {
        byte[] response = Encoding.ASCII.GetBytes("Hello, World!");
        bool IHttpModule.HandleRequest(ServerSide.Server server, HttpHandler handler, ClientData client, HttpRequest request, ref HttpResponse response)
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

        void IHttpModule.Tick(ServerSide.Server server, HttpHandler handler)
        {
            
        }
    }
}
