using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Server.Handlers
{
    /// <summary>
    /// Broadcast handler.
    /// </summary>
    public class BroadcastHandler : IHandler
    {
        private Server managedServer;

        public BroadcastHandler(Server server)
        {
            this.managedServer = server;
        }

        public void ClientRemoved(ClientStruct client)
        {
            
        }

        public bool HandleClient(ClientStruct client)
        {
            return false;
        }

        public bool HandleReceivedMessage(ClientStruct client, Message message)
        {
            managedServer.LogDebug("Broadcast: " + UnicodeEncoding.Unicode.GetString(message.MessageBytes));
            managedServer.LogDebug("From: " + client.TcpClient.Client.RemoteEndPoint.ToString());
            foreach(ClientStruct clientStruct in managedServer.Clients)
            {
                managedServer.QueueMessage(clientStruct, message.MessageBytes);
            }
            return false;
        }

        public bool HandleSendMessage(ClientStruct client, ref Message message)
        {
            return false;
        }

        public bool PreHandleReceivedMessage(ClientStruct client, ref Message message)
        {
            return false;
        }

        public void Tick()
        {
            
        }
    }
}
