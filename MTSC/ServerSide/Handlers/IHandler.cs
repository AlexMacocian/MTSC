using System;

namespace MTSC.ServerSide.Handlers
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
        /// <param name="server">Server calling the handler.</param>
        /// <returns>True if the handler processed the client.</returns>
        bool HandleClient(Server server, ClientData client);
        /// <summary>
        /// Handles a message before sending.
        /// </summary>
        /// <param name="client">Client object.</param>
        /// <param name="message">Message to be processed.</param>
        /// <param name="server">Server calling the handler.</param>
        /// <returns>True if no other handler should handle this message.</returns>
        bool HandleSendMessage(Server server, ClientData client, ref Message message);
        /// <summary>
        /// Called before the message handling.
        /// Perform here any processing of the message.
        /// </summary>
        /// <param name="client">Client structure.</param>
        /// <param name="message">Message to be preprocessed.</param>
        /// <param name="server">Server calling the handler.</param>
        /// <returns>True if the message has been preprocessed and no other handler should handle it anymore.</returns>
        bool PreHandleReceivedMessage(Server server, ClientData client, ref Message message);
        /// <summary>
        /// Handles the received message.
        /// </summary>
        /// <param name="message">Message to be handled.</param>
        /// <param name="server">Server calling the handler.</param>
        /// <returns>True if the message has been handled, false if the message has not been handled.</returns>
        bool HandleReceivedMessage(Server server, ClientData client, Message message);
        /// <summary>
        /// Handles the removal of a client from the server.
        /// </summary>
        /// <param name="client">Client about to be removed.</param>
        /// <param name="server">Server calling the handler.</param>
        void ClientRemoved(Server server, ClientData client);
        /// <summary>
        /// Method performs regular operations onto the server.
        /// </summary>
        /// <param name="server">Server calling the handler.</param>
        void Tick(Server server);
    }
}
