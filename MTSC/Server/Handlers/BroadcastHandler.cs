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
        public BroadcastHandler()
        {

        }

        void IHandler.ClientRemoved(Server server, ClientStruct client)
        {
            
        }

        bool IHandler.HandleClient(Server server, ClientStruct client)
        {
            return false;
        }

        bool IHandler.HandleReceivedMessage(Server server, ClientStruct client, Message message)
        {
            server.LogDebug("Broadcast: " + UnicodeEncoding.Unicode.GetString(message.MessageBytes));
            server.LogDebug("From: " + client.TcpClient.Client.RemoteEndPoint.ToString());
            foreach(ClientStruct clientStruct in server.Clients)
            {
                server.QueueMessage(clientStruct, message.MessageBytes);
            }
            return false;
        }

        bool IHandler.HandleSendMessage(Server server, ClientStruct client, ref Message message)
        {
            return false;
        }

        bool IHandler.PreHandleReceivedMessage(Server server, ClientStruct client, ref Message message)
        {
            return false;
        }

        void IHandler.Tick(Server server)
        {
            
        }
    }
}
