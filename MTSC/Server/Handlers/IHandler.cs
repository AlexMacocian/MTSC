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
        bool HandleClient(TcpClient client);
        /// <summary>
        /// Handles the received message.
        /// </summary>
        /// <param name="message">Message to be handled.</param>
        /// <returns>True if the message has been handled, false if the message has not been handled.</returns>
        bool HandleMessage(out Message message);
    }
}
