using MTSC.Server;
using MTSC.Server.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Common.WebSockets.ServerModules
{
    public interface IWebsocketModule
    {
        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="handler">Handler that currently processes the message.</param>
        /// <param name="messageBytes">Bytes of the message.</param>
        /// <param name="client">Client data.</param>
        /// <returns>True if no other module should process the message.</returns>
        bool HandleReceivedMessage(IHandler handler, ClientData client, byte[] messageBytes);
    }
}
