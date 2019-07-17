using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MTSC.Client.Handlers
{
    /// <summary>
    /// Handler interface for client communication.
    /// </summary>
    public interface IHandler
    {
        /// <summary>
        /// Called when the connection is being initialized.
        /// </summary>
        /// <param name="client">Client object.</param>
        /// <returns>True if the initialization is successful.</returns>
        /// <remarks>If this method returns false, the connection will fail and return false.</remarks>
        bool InitializeConnection(TcpClient client);
        /// <summary>
        /// Called when a message is being sent to the server.
        /// </summary>
        /// <param name="client">Client socket.</param>
        /// <param name="message">Message to be sent.</param>
        /// <returns></returns>
        bool OnSend(TcpClient client, out Message message);
        /// <summary>
        /// Called before the message is handled.
        /// Use this method to modify the message if necesarry.
        /// </summary>
        /// <param name="client">Client object.</param>
        /// <param name="message">Message to be preprocessed.</param>
        /// <returns>True if the message has been preprocessed and no other handler should modify the message.</returns>
        bool PreHandleReceivedMessage(TcpClient client, out Message message);
        /// <summary>
        /// Called when a message has been received.
        /// </summary>
        /// <param name="client">Client object.</param>
        /// <param name="message">Message to be handled.</param>
        /// <returns>True if the message has been handled and no other handler should process it.</returns>
        bool HandleReceivedMessage(TcpClient client, Message message);
        /// <summary>
        /// Called every cycle. This method should perform regular actions on the connection.
        /// </summary>
        /// <param name="client">Client object.</param>
        void Tick(TcpClient client);
        /// <summary>
        /// Called when the client is disconnected.
        /// </summary>
        /// <param name="client">Client object.</param>
        void OnDisconnect(TcpClient client);
    }
}
