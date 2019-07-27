using MTSC.Common.Http;
using MTSC.Common.WebSockets;
using MTSC.Common.WebSockets.ServerModules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MTSC.Server.Handlers
{
    public class WebsocketHandler : IHandler
    {
        private static string WebsocketHeaderAcceptKey = "Sec-WebSocket-Accept";
        private static string WebsocketHeaderKey = "Sec-WebSocket-Key";
        private static string WebsocketProtocolKey = "Sec-WebSocket-Protocol";
        private static string WebsocketProtocolVersionKey = "Sec-WebSocket-Version";
        private static string GlobalUniqueIdentifier = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static SHA1 sha1Provider = SHA1.Create();
        private static RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        public enum SocketState
        {
            Initial,
            Handshaking,
            Established,
            Closed
        }
        #region Fields
        public ConcurrentDictionary<ClientData, SocketState> webSockets = new ConcurrentDictionary<ClientData, SocketState>();
        ConcurrentQueue<Tuple<ClientData,WebsocketMessage>> messageQueue = new ConcurrentQueue<Tuple<ClientData, WebsocketMessage>>();
        List<IWebsocketModule> websocketModules = new List<IWebsocketModule>();
        #endregion
        #region Public Methods
        /// <summary>
        /// Add a webSocket module onto the server.
        /// </summary>
        /// <param name="module">Module to be added.</param>
        /// <returns>This handler object.</returns>
        public WebsocketHandler AddWebsocketHandler(IWebsocketModule module)
        {
            this.websocketModules.Add(module);
            return this;
        }
        /// <summary>
        /// Send a message to the client.
        /// </summary>
        /// <param name="message">Message to be sent to client.</param>
        public void QueueMessage(ClientData client, byte[] message)
        {
            WebsocketMessage sendMessage = new WebsocketMessage();
            sendMessage.Data = message;
            sendMessage.FIN = true;
            rng.GetBytes(sendMessage.Mask);
            sendMessage.Opcode = WebsocketMessage.Opcodes.Text;
            messageQueue.Enqueue(new Tuple<ClientData, WebsocketMessage>(client, sendMessage));
        }
        /// <summary>
        /// Send a message to the client.
        /// </summary>
        /// <param name="client">Client object.</param>
        /// <param name="message">Message packet.</param>
        public void QueueMessage(ClientData client, WebsocketMessage message)
        {
            messageQueue.Enqueue(new Tuple<ClientData, WebsocketMessage>(client, message));
        }
        /// <summary>
        /// Signals that the connetion is closing.
        /// </summary>
        /// <param name="client">Client to be disconnected.</param>
        public void CloseConnection(ClientData client)
        {
            WebsocketMessage websocketMessage = new WebsocketMessage();
            websocketMessage.FIN = true;
            websocketMessage.Opcode = WebsocketMessage.Opcodes.Close;
            websocketMessage.Masked = false;
            QueueMessage(client, websocketMessage);
        }
        #endregion
        #region Handler Implementation
        void IHandler.ClientRemoved(Server server, ClientData client)
        {
            SocketState state = SocketState.Initial;
            while(!webSockets.TryRemove(client, out state));
        }

        bool IHandler.HandleClient(Server server, ClientData client)
        {
            webSockets[client] = SocketState.Initial;
            return false;
        }

        bool IHandler.HandleReceivedMessage(Server server, ClientData client, Message message)
        {
            if (webSockets[client] == SocketState.Initial)
            {
                HttpMessage request = new HttpMessage();
                request.ParseRequest(message.MessageBytes);
                if(request.Method == HttpMessage.MethodEnum.Get && request.ContainsHeader(HttpMessage.GeneralHeadersEnum.Connection) &&
                    request[HttpMessage.GeneralHeadersEnum.Connection].ToLower() == "upgrade" && request.ContainsHeader(WebsocketProtocolVersionKey) &&
                    request[WebsocketProtocolVersionKey] == "13")
                {
                    /*
                     * Prepare the handshake string.
                     */
                    string base64Key = request[WebsocketHeaderKey];
                    base64Key = base64Key.Trim();
                    string handshakeKey = base64Key + GlobalUniqueIdentifier;
                    string returnBase64Key = Convert.ToBase64String(sha1Provider.ComputeHash(Encoding.UTF8.GetBytes(handshakeKey)));

                    /*
                     * Prepare the response.
                     */
                    HttpMessage response = new HttpMessage();
                    response.StatusCode = HttpMessage.StatusCodes.SwitchingProtocols;
                    response[HttpMessage.GeneralHeadersEnum.Upgrade] = "websocket";
                    response[HttpMessage.GeneralHeadersEnum.Connection] = "Upgrade";
                    response[WebsocketHeaderAcceptKey] = returnBase64Key;
                    server.QueueMessage(client, response.GetResponse(true));
                    webSockets[client] = SocketState.Established;
                    server.LogDebug("Websocket initialized " + client.TcpClient.Client.RemoteEndPoint.ToString());
                    foreach (IWebsocketModule websocketModule in websocketModules)
                    {
                        websocketModule.ConnectionInitialized(server, this, client);
                    }
                    return true;
                }
            }
            else if(webSockets[client] == SocketState.Established)
            {
                WebsocketMessage receivedMessage = new WebsocketMessage(message.MessageBytes);
                if (receivedMessage.Opcode == WebsocketMessage.Opcodes.Close)
                {
                    foreach (IWebsocketModule websocketModule in websocketModules)
                    {
                        websocketModule.ConnectionClosed(server, this, client);
                    }
                    client.ToBeRemoved = true;
                    SocketState outSocketState = SocketState.Closed;
                    while(!webSockets.TryRemove(client, out outSocketState));
                }
                else
                {
                    foreach (IWebsocketModule websocketModule in websocketModules)
                    {
                        if (websocketModule.HandleReceivedMessage(server, this, client, receivedMessage))
                        {
                            break;
                        }
                    }
                }
                return true;
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
            while (messageQueue.Count > 0)
            {
                Tuple<ClientData, WebsocketMessage> tuple = null;
                if (messageQueue.TryDequeue(out tuple))
                {
                    server.QueueMessage(tuple.Item1, tuple.Item2.GetMessageBytes());
                    if(tuple.Item2.Opcode == WebsocketMessage.Opcodes.Close)
                    {
                        foreach (IWebsocketModule websocketModule in websocketModules)
                        {
                            websocketModule.ConnectionClosed(server, this, tuple.Item1);
                        }
                        tuple.Item1.ToBeRemoved = true;
                        SocketState outSocketState = SocketState.Closed;
                        while(!webSockets.TryRemove(tuple.Item1, out outSocketState));
                    }
                }
            }
        }
        #endregion
    }
}
