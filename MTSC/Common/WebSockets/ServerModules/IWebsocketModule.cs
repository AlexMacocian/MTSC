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
        /// Called when a connection has been initialized. 
        /// </summary>
        /// <param name="server">Server object.</param>
        /// <param name="handler">Handler currently processing.</param>
        /// <param name="client">Client object.</param>
        void ConnectionInitialized(Server.Server server, WebsocketHandler handler, ClientData client);
        /// <summary>
        /// Handle a received message.
        /// </summary>
        /// <param name="handler">Handler that currently processes the message.</param>
        /// <param name="messageBytes">Bytes of the message.</param>
        /// <param name="client">Client data.</param>
        /// <returns>True if no other module should process the message.</returns>
        bool HandleReceivedMessage(Server.Server server, WebsocketHandler handler, ClientData client, WebsocketMessage receivedMessage);
        /// <summary>
        /// Called when a connection has been closed.
        /// </summary>
        /// <param name="server">Server object.</param>
        /// <param name="handler">Handler currently processing.</param>
        /// <param name="client">Client object.</param>
        void ConnectionClosed(Server.Server server, WebsocketHandler handler, ClientData client);
    }
}
