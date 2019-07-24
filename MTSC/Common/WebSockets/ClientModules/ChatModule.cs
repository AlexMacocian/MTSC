using System;
using System.Collections.Generic;
using System.Text;
using MTSC.Client.Handlers;

namespace MTSC.Common.WebSockets.ClientModules
{
    public class ChatModule : IWebsocketModule
    {
        #region Public Methods
        public void SendMessage(WebsocketHandler websocketHandler, string message)
        {
            byte[] encodedMessage = WebsocketHelper.EncodeMessage(message);
            websocketHandler.QueueMessage(encodedMessage);
        }
        #endregion
        #region Interface Implementation
        bool IWebsocketModule.HandleReceivedMessage(Client.Client client, IHandler handler, byte[] messageBytes)
        {
            string receivedMessage = WebsocketHelper.DecodeMessage(messageBytes);
            client.Log(">" + receivedMessage);
            return false;
        }
        #endregion
    }
}
