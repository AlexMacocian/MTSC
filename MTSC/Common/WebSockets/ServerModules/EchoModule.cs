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
        public void SendMessage(WebsocketHandler handler, ClientData client, string message)
        {
            byte[] encodedMessage = WebsocketHelper.EncodeMessage(message);
            handler.QueueMessage(client, encodedMessage);
        }
        #endregion
        #region Interface Implementation
        bool IWebsocketModule.HandleReceivedMessage(IHandler handler, ClientData client, byte[] messageBytes)
        {
            string receivedMessage = WebsocketHelper.DecodeMessage(messageBytes);
            SendMessage((WebsocketHandler)handler, client, receivedMessage);
            return false;
        }
        #endregion
    }
}
