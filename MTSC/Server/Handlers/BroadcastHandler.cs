using System.Text;

namespace MTSC.Server.Handlers
{
    /// <summary>
    /// Broadcast handler.
    /// </summary>
    public sealed class BroadcastHandler : IHandler
    {
        public BroadcastHandler()
        {

        }

        void IHandler.ClientRemoved(Server server, ClientData client)
        {
            
        }

        bool IHandler.HandleClient(Server server, ClientData client)
        {
            return false;
        }

        bool IHandler.HandleReceivedMessage(Server server, ClientData client, Message message)
        {
            server.LogDebug("Broadcast: " + UnicodeEncoding.Unicode.GetString(message.MessageBytes));
            server.LogDebug("From: " + client.TcpClient.Client.RemoteEndPoint.ToString());
            foreach(ClientData clientStruct in server.Clients)
            {
                server.QueueMessage(clientStruct, message.MessageBytes);
            }
            return false;
        }

        bool IHandler.HandleSendMessage(Server server, ClientData client, ref Message message)
        {
            return false;
        }

        bool IHandler.PreHandleReceivedMessage(Server server, ClientData client, ref Message message)
        {
            return false;
        }

        void IHandler.Tick(Server server)
        {
            
        }
    }
}
