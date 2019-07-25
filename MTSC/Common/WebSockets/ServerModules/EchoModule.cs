using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MTSC.Server;
using MTSC.Server.Handlers;

namespace MTSC.Common.WebSockets.ServerModules
{
    public class EchoModule : IWebsocketModule
    {
        #region Public Methods
        public void SendMessage(WebsocketHandler handler, ClientData client, WebsocketMessage message)
        {
            byte[] encodedMessage = message.GetMessageBytes();
            handler.QueueMessage(client, encodedMessage);
        }
        #endregion
        #region Interface Implementation
        bool IWebsocketModule.HandleReceivedMessage(Server.Server server, IHandler handler, ClientData client, WebsocketMessage receivedMessage)
        {
            SendMessage((WebsocketHandler)handler, client, receivedMessage);
            return false;
        }

        void IWebsocketModule.ConnectionClosed(Server.Server server, IHandler handler, ClientData client)
        {
            throw new NotImplementedException();
        }

        void IWebsocketModule.ConnectionInitialized(Server.Server server, IHandler handler, ClientData client)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
