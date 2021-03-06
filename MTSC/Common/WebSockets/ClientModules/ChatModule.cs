﻿using System.Security.Cryptography;
using System.Text;
using MTSC.Client.Handlers;

namespace MTSC.Common.WebSockets.ClientModules
{
    public sealed class ChatModule : IWebsocketModule
    {
        static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        #region Public Methods
        public void SendMessage(WebsocketHandler websocketHandler, string message)
        {
            WebsocketMessage websocketMessage = new WebsocketMessage();
            websocketMessage.Data = Encoding.UTF8.GetBytes(message);
            websocketMessage.Opcode = WebsocketMessage.Opcodes.Text;
            websocketMessage.FIN = true;
            websocketMessage.Masked = true;
            rng.GetBytes(websocketMessage.Mask);
            websocketHandler.QueueMessage(websocketMessage);
        }
        #endregion
        #region Interface Implementation
        bool IWebsocketModule.HandleReceivedMessage(Client.Client client, WebsocketHandler handler, WebsocketMessage receivedMessage)
        {
            string messageString = Encoding.UTF8.GetString(receivedMessage.Data);
            client.Log(">" + messageString);
            return false;
        }
        void IWebsocketModule.ConnectionClosed(Client.Client client, WebsocketHandler handler)
        {
            
        }

        void IWebsocketModule.ConnectionInitialized(Client.Client client, WebsocketHandler handler)
        {
            
        }
        #endregion
    }
}
