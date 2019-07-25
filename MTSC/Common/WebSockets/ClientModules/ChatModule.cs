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
            byte[] encodedMessage = WebsocketHelper.EncodeTextMessage(message, true);
            websocketHandler.QueueMessage(encodedMessage);
        }
        #endregion
        #region Interface Implementation
        bool IWebsocketModule.HandleReceivedMessage(Client.Client client, IHandler handler, WebsocketMessage receivedMessage)
        {
            string messageString = Encoding.UTF8.GetString(receivedMessage.Data);
            client.Log(">" + messageString);
            return false;
        }
        #endregion
    }
}
