using MTSC.Client.Handlers;
using MTSC.Common.Http;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MTSC.Common.Http.ClientModules
{
    public interface IHttpModule
    {
        /// <summary>
        /// Handle a response received from the server.
        /// </summary>
        /// <param name="handler">Handler that operates on the response.</param>
        /// <param name="response">Response message.</param>
        /// <returns>True if no other module should operate on the response.</returns>
        bool HandleResponse(IHandler handler, HttpMessage response);
    }
}
