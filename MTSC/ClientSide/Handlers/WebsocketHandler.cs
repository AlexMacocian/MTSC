using MTSC.Common.Http;
using MTSC.Common.WebSockets;
using MTSC.Common.WebSockets.ClientModules;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MTSC.Client.Handlers
{
    /// <summary>
    /// Handler implementing websocket protocol.
    /// </summary>
    public sealed class WebsocketHandler : IHandler
    {
        private static string WebsocketHeaderAcceptKey = "Sec-WebSocket-Accept";
        private static string WebsocketHeaderKey = "Sec-WebSocket-Key";
        private static string WebsocketProtocolKey = "Sec-WebSocket-Protocol";
        private static string WebsocketProtocolVersionKey = "Sec-WebSocket-Version";
        private static string GlobalUniqueIdentifier = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static SHA1 sha1Provider = SHA1.Create();
        private enum SocketState
        {
            Initial,
            Handshaking,
            Established,
            Closed
        }

        #region Fields
        SocketState state = SocketState.Initial;
        Queue<WebsocketMessage> messageQueue = new Queue<WebsocketMessage>();
        List<IWebsocketModule> websocketModules = new List<IWebsocketModule>();
        string expectedguid = string.Empty;
        #endregion
        #region Properties
        public string WebsocketURI { get; set; }
        #endregion
        #region Constructors
        public WebsocketHandler()
        {
            WebsocketURI = "/";
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Add a module to the websocket handler.
        /// </summary>
        /// <param name="websocketModule">Module to be added.</param>
        /// <returns>This handler object.</returns>
        public WebsocketHandler AddModule(IWebsocketModule websocketModule)
        {
            websocketModules.Add(websocketModule);
            return this;
        }
        /// <summary>
        /// Add a message to the queue to be sent.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        public void QueueMessage(WebsocketMessage message)
        {
            messageQueue.Enqueue(message);
        }
        #endregion
        #region Handler Implementation
        void IHandler.Disconnected(Client client)
        {
            
        }

        bool IHandler.HandleReceivedMessage(Client client, Message message)
        {
            if(state == SocketState.Handshaking)
            {
                HttpMessage response = new HttpMessage();
                response.ParseResponse(message.MessageBytes);
                if(response.StatusCode == HttpMessage.StatusCodes.SwitchingProtocols &&
                    response["Upgrade"] == "websocket" &&
                    response[HttpMessage.GeneralHeaders.Connection].ToLower() == "upgrade" &&
                    response[WebsocketHeaderAcceptKey].Trim() == expectedguid)
                {
                    state = SocketState.Established;
                    foreach (IWebsocketModule websocketModule in websocketModules)
                    {
                        websocketModule.ConnectionInitialized(client, this);
                    }
                }
                return true;
            }
            else if(state == SocketState.Established)
            {
                WebsocketMessage receivedMessage = new WebsocketMessage(message.MessageBytes);
                if (receivedMessage.Opcode == WebsocketMessage.Opcodes.Close)
                {
                    foreach (IWebsocketModule websocketModule in websocketModules)
                    {
                        websocketModule.ConnectionClosed(client, this);
                    }
                }
                else
                {
                    foreach (IWebsocketModule websocketModule in websocketModules)
                    {
                        if (websocketModule.HandleReceivedMessage(client, this, receivedMessage))
                        {
                            break;
                        }
                    }
                }
                return true;
            }
            return false;
        }

        bool IHandler.HandleSendMessage(Client client, ref Message message)
        {
            return false;
        }

        bool IHandler.InitializeConnection(Client client)
        {
            state = SocketState.Handshaking;
            string handshakeGuid = Guid.NewGuid().ToString();
            string handshakeKey = handshakeGuid+ GlobalUniqueIdentifier;
            expectedguid = Convert.ToBase64String(sha1Provider.ComputeHash(Encoding.UTF8.GetBytes(handshakeKey)));
            HttpMessage beginRequest = new HttpMessage();
            beginRequest.Method = HttpMessage.HttpMethods.Get;
            beginRequest.RequestURI = WebsocketURI;
            beginRequest[HttpMessage.RequestHeaders.Host] = client.Address;
            beginRequest[HttpMessage.GeneralHeaders.Connection] = "Upgrade";
            beginRequest[WebsocketHeaderKey] = handshakeGuid;
            beginRequest["Origin"] = client.Address;
            beginRequest[WebsocketProtocolKey] = "chat";
            beginRequest[WebsocketProtocolVersionKey] = "13";
            client.QueueMessage(beginRequest.BuildRequest());
            return true;
        }

        bool IHandler.PreHandleReceivedMessage(Client client, ref Message message)
        {
            return false;
        }

        void IHandler.Tick(Client client)
        {
            while(messageQueue.Count > 0)
            {
                WebsocketMessage message = messageQueue.Dequeue();
                client.QueueMessage(message.GetMessageBytes());
                if(message.Opcode == WebsocketMessage.Opcodes.Close)
                {
                    foreach (IWebsocketModule websocketModule in websocketModules)
                    {
                        websocketModule.ConnectionClosed(client, this);
                    }
                }
            }
        }
        #endregion
    }
}
