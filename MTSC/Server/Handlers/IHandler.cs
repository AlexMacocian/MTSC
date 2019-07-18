using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MTSC.Server.Handlers
{
    /// <summary>
    /// Interface for communication handlers.
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// Handles a new client.
        /// </summary>
        /// <param name="client">Client to be handled.</param>
        /// <returns>True if the handler processed the client.</returns>
        bool HandleClient(ClientStruct client);
        /// <summary>
        /// Called before the message handling.
        /// Perform here any processing of the message.
        /// </summary>
        /// <param name="client">Client structure.</param>
        /// <param name="message">Message to be preprocessed.</param>
        /// <returns>True if the message has been preprocessed and no other handler should handle it anymore.</returns>
        bool PreHandleMessage(ClientStruct client, ref Message message);
        /// <summary>
        /// Handles the received message.
        /// </summary>
        /// <param name="message">Message to be handled.</param>
        /// <returns>True if the message has been handled, false if the message has not been handled.</returns>
        bool HandleMessage(ClientStruct client, Message message);
        /// <summary>
        /// Handles the removal of a client from the server.
        /// </summary>
        /// <param name="client">Client about to be removed.</param>
        void ClientRemoved(ClientStruct client);
        /// <summary>
        /// Method performs regular operations onto the server.
        /// </summary>
        void Tick();
    }
}
