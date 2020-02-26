using MTSC.Server;
using MTSC.Server.Handlers;

namespace MTSC.Common.WebSockets.ServerModules
{
    public sealed class EchoModule : IWebsocketModule
    {
        #region Public Methods
        public void SendMessage(WebsocketHandler handler, ClientData client, WebsocketMessage message)
        {
            handler.QueueMessage(client, message.Data, WebsocketMessage.Opcodes.Binary);
        }
        #endregion
        #region Interface Implementation
        bool IWebsocketModule.HandleReceivedMessage(Server.Server server, WebsocketHandler handler, ClientData client, WebsocketMessage receivedMessage)
        {
            SendMessage(handler, client, receivedMessage);
            return false;
        }

        void IWebsocketModule.ConnectionClosed(Server.Server server, WebsocketHandler handler, ClientData client)
        {
            
        }

        void IWebsocketModule.ConnectionInitialized(Server.Server server, WebsocketHandler handler, ClientData client)
        {
            
        }
        #endregion
    }
}
