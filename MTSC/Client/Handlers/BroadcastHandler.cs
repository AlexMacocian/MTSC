using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MTSC.Client.Handlers
{
    public class BroadcastHandler : IHandler
    {
        private List<string> buffer = new List<string>();
        /// <summary>
        /// Creates a new instance of BroadcastHandler.
        /// </summary>
        public BroadcastHandler()
        {

        }

        /// <summary>
        /// Broadcast a message to all other clients connected to the server.
        /// </summary>
        /// <param name="message">Message to be broadcasted.</param>
        /// <param name="client">Client object containing the communication channel.</param>
        public void Broadcast(Client client, string message)
        {
            client.QueueMessage(UnicodeEncoding.Unicode.GetBytes(message));
        }

        void IHandler.Disconnected(Client client)
        {
            
        }

        bool IHandler.HandleReceivedMessage(Client client, Message message)
        {
            client.LogDebug("Broadcast: " + UnicodeEncoding.Unicode.GetString(message.MessageBytes));
            client.Log(">" + UnicodeEncoding.Unicode.GetString(message.MessageBytes));
            return false;
        }

        bool IHandler.HandleSendMessage(Client client, ref Message message)
        {
            return false;
        }

        bool IHandler.InitializeConnection(Client client)
        {
            return true;
        }

        bool IHandler.PreHandleReceivedMessage(Client client, ref Message message)
        {
            return false;
        }

        void IHandler.Tick(Client client)
        {
            
        }
    }
}
