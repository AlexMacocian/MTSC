using MTSC.Common.Http;
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
        private enum SocketState
        {
            Initial,
            Handshaking,
            Established,
            Closed
        }
        ConcurrentDictionary<ClientData, SocketState> webSockets = new ConcurrentDictionary<ClientData, SocketState>();
        ConcurrentQueue<Tuple<ClientData, byte[]>> messageQueue = new ConcurrentQueue<Tuple<ClientData, byte[]>>();
        List<IWebsocketModule> websocketModules = new List<IWebsocketModule>();

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
            messageQueue.Enqueue(new Tuple<ClientData, byte[]>(client, message));
        }
        #endregion
        #region Handler Implementation
        void IHandler.ClientRemoved(Server server, ClientData client)
        {
            SocketState state = SocketState.Initial;
            webSockets.TryRemove(client, out state);
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
                if(request.Method == HttpMessage.MethodEnum.Get && 
                    request[HttpMessage.GeneralHeadersEnum.Connection].ToLower() == "upgrade" &&
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
                    return true;
                }
            }
            else if(webSockets[client] == SocketState.Established)
            {
                foreach(IWebsocketModule websocketModule in websocketModules)
                {
                    if(websocketModule.HandleReceivedMessage(this, client, message.MessageBytes))
                    {
                        break;
                    }
                }
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
                Tuple<ClientData, byte[]> tuple = null;
                if (messageQueue.TryDequeue(out tuple))
                {
                    server.QueueMessage(tuple.Item1, tuple.Item2);
                }
            }
        }
        #endregion
    }
}
