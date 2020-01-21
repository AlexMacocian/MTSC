using System;
using System.Collections.Generic;
using System.Text;
using MTSC.Server;
using MTSC.Server.Handlers;

namespace MTSC.Common.Http.ServerModules
{
    /// <summary>
    /// Simple module that returns status code 404.
    /// </summary>
    public class Http404Module : IHttpModule
    {
        /// <summary>
        /// Check the request and construct the response.
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="client"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns>True so no other handler modifies the response, so the response contains the 404 return status code.</returns>
        bool IHttpModule.HandleRequest(Server.Server server, HttpHandler handler, ClientData client, HttpRequest request, ref HttpResponse response)
        {
            if(request.Method == HttpMessage.HttpMethods.Get)
            {
                //client.ToBeRemoved = true;
                response.StatusCode = HttpMessage.StatusCodes.NotFound;
                response.Headers[HttpMessage.GeneralHeaders.Date] = DateTime.Now.ToString();
            }
            return true;
        }

        void IHttpModule.Tick(Server.Server server, HttpHandler handler)
        {
            
        }
    }
}
