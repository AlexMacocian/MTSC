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
        bool IHttpModule.HandleRequest(HttpHandler handler, ClientData client, HttpMessage request)
        {
            if(request.Method == HttpMessage.MethodEnum.Get)
            {
                HttpMessage response = new HttpMessage();
                if (request.ContainsHeader(HttpMessage.GeneralHeadersEnum.Connection) &&
                    request[HttpMessage.GeneralHeadersEnum.Connection] == "Keep-Alive")
                {
                    response[HttpMessage.GeneralHeadersEnum.Connection] = "Keep-Alive";
                }
                else
                {
                    client.ToBeRemoved = true;
                }
                response.StatusCode = HttpMessage.StatusCodes.NotFound;
                response[HttpMessage.GeneralHeadersEnum.Date] = DateTime.Now.ToString();
                handler.SendResponse(client, response);
            }
            return false;
        }
    }
}
