using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.Common.WebSockets.ServerModules
{
    public sealed class BroadcastModule : IWebsocketModule
    {
        void IWebsocketModule.ConnectionClosed(ServerSide.Server server, WebsocketHandler handler, ClientData client)
        {
            
        }

        void IWebsocketModule.ConnectionInitialized(ServerSide.Server server, WebsocketHandler handler, ClientData client)
        {
            
        }

        bool IWebsocketModule.HandleReceivedMessage(ServerSide.Server server, WebsocketHandler handler, ClientData client, WebsocketMessage receivedMessage)
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
