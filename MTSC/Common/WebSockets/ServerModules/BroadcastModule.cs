using MTSC.Server;
using MTSC.Server.Handlers;

namespace MTSC.Common.WebSockets.ServerModules
{
    public sealed class BroadcastModule : IWebsocketModule
    {
        void IWebsocketModule.ConnectionClosed(Server.Server server, WebsocketHandler handler, ClientData client)
        {
            
        }

        void IWebsocketModule.ConnectionInitialized(Server.Server server, WebsocketHandler handler, ClientData client)
        {
            
        }

        bool IWebsocketModule.HandleReceivedMessage(Server.Server server, WebsocketHandler handler, ClientData client, WebsocketMessage receivedMessage)
        {
            receivedMessage.Masked = false;
            //receivedMessage.Opcode = WebsocketMessage.Opcodes.Text;
            foreach(ClientData otherClient in handler.webSockets.Keys)
            {
                handler.QueueMessage(otherClient, receivedMessage);
            }
            return false;
        }
    }
}
