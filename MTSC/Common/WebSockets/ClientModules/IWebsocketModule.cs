using MTSC.Client.Handlers;
using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Common.WebSockets.ClientModules
{
    /// <summary>
    /// Interface for websocket modules.
    /// </summary>
    public interface IWebsocketModule
    {
        /// <summary>
        /// Called when a new websocket connection has been initialized.
        /// </summary>
        /// <param name="client">Client object.</param>
        /// <param name="handler">Handler that performed the initialization.</param>
        void ConnectionInitialized(Client.Client client, IHandler handler);
        /// <summary>
        /// Handle received message.
        /// </summary>
        /// <param name="client">Client object.</param>
        /// <param name="handler">Handler currently processing the message.</param>
        /// <param name="messageBytes">Array containing the message.</param>
        /// <returns>True if no other module should handle this message.</returns>
        bool HandleReceivedMessage(Client.Client client, IHandler handler, WebsocketMessage messageBytes);
        /// <summary>
        /// Called when an existing websocket connection is being closed.
        /// </summary>
        /// <param name="client">Client object.</param>
        /// <param name="handler">Handler that closes the connection.</param>
        void ConnectionClosed(Client.Client client, IHandler handler);
    }
}
