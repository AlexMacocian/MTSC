using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MTSC.Client.Handlers
{
    public class BroadcastHandler : IHandler
    {
        private List<string> buffer = new List<string>();
        private Client managedClient;
        /// <summary>
        /// Creates a new instance of BroadcastHandler.
        /// </summary>
        /// <param name="client">Client managed by the handler.</param>
        public BroadcastHandler(Client client)
        {
            this.managedClient = client;
        }

        /// <summary>
        /// Broadcast a message to all other clients connected to the server.
        /// </summary>
        /// <param name="message">Message to be broadcasted.</param>
        public void Broadcast(string message)
        {
            managedClient.QueueMessage(UnicodeEncoding.Unicode.GetBytes(message));
        }

        public void Disconnected(TcpClient client)
        {
            
        }

        public bool HandleReceivedMessage(TcpClient client, Message message)
        {
            managedClient.LogDebug("Broadcast: " + UnicodeEncoding.Unicode.GetString(message.MessageBytes));
            managedClient.Log(">" + UnicodeEncoding.Unicode.GetString(message.MessageBytes));
            return false;
        }

        public bool HandleSendMessage(TcpClient client, ref Message message)
        {
            return false;
        }

        public bool InitializeConnection(TcpClient client)
        {
            return true;
        }

        public bool PreHandleReceivedMessage(TcpClient client, ref Message message)
        {
            return false;
        }

        public void Tick(TcpClient tcpClient)
        {
            
        }
    }
}
