using MTSC.Common;
using MTSC.Server;
using MTSC.Server.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Common.Http.ServerModules
{
    /// <summary>
    /// Interface for Http modules used by the server http handler.
    /// </summary>
    public interface IHttpModule
    {
        /// <summary>
        /// Handle the received request.
        /// </summary>
        /// <param name="senderHandler">Handler that handled the request.</param>
        /// <param name="client">Client data.</param>
        /// <param name="request">Request message.</param>
        /// <returns>True if no other module should handle the received request.</returns>
        bool HandleRequest(IHandler handler, ClientStruct client, HttpMessage request);
    }
}
