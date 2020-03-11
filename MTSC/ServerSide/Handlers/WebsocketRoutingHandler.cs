using MTSC.Common.Http;
using MTSC.Common.WebSockets;
using MTSC.Common.WebSockets.RoutingModules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace MTSC.ServerSide.Handlers
{
    public class WebsocketRoutingHandler : IHandler
    {
        private static Func<Server, HttpRequest, ClientData, RouteEnablerResponse> alwaysEnabled = (server, message, client) => RouteEnablerResponse.Accept;
        private static readonly string WebsocketHeaderAcceptKey = "Sec-WebSocket-Accept";
        private static readonly string WebsocketHeaderKey = "Sec-WebSocket-Key";
        private static readonly string WebsocketProtocolKey = "Sec-WebSocket-Protocol";
        private static readonly string WebsocketProtocolVersionKey = "Sec-WebSocket-Version";
        private static readonly string GlobalUniqueIdentifier = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
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
        Dictionary<string, (WebsocketRouteBase, Func<Server, HttpRequest, ClientData, RouteEnablerResponse>)> moduleDictionary =
            new Dictionary<string, (WebsocketRouteBase, Func<Server, HttpRequest, ClientData, RouteEnablerResponse>)>();
        ConcurrentDictionary<ClientData, WebsocketRouteBase> routingTable = new ConcurrentDictionary<ClientData, WebsocketRouteBase>();
        ConcurrentQueue<Tuple<ClientData, WebsocketMessage>> messageQueue = new ConcurrentQueue<Tuple<ClientData, WebsocketMessage>>();
        #endregion
        #region Public Methods
        public WebsocketRoutingHandler AddRoute(string uri, WebsocketRouteBase module)
        {
            this.moduleDictionary.Add(uri, (module, alwaysEnabled));
            return this;
        }

        public WebsocketRoutingHandler AddRoute(
            string uri,
            WebsocketRouteBase module,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
        {
            this.moduleDictionary.Add(uri, (module, routeEnabler));
            return this;
        }

        public WebsocketRoutingHandler RemoveRoute(string uri)
        {
            moduleDictionary.Remove(uri);
            return this;
        }

        public void QueueMessage(ClientData client, byte[] message, WebsocketMessage.Opcodes opcode = WebsocketMessage.Opcodes.Text)
        {
            WebsocketMessage sendMessage = new WebsocketMessage();
            sendMessage.Data = message;
            sendMessage.FIN = true;
            sendMessage.Masked = false;
            rng.GetBytes(sendMessage.Mask);
            sendMessage.Opcode = opcode;
            messageQueue.Enqueue(new Tuple<ClientData, WebsocketMessage>(client, sendMessage));
        }

        public void QueueMessage(ClientData client, WebsocketMessage message)
        {
            messageQueue.Enqueue(new Tuple<ClientData, WebsocketMessage>(client, message));
        }

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
            while (webSockets.ContainsKey(client))
            {
                webSockets.TryRemove(client, out state);
            }
            routingTable[client].CallConnectionClosed(server, this, client);
            while (routingTable.ContainsKey(client))
            {
                routingTable.TryRemove(client, out _);
            }
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
                PartialHttpRequest request;
                try
                {
                    request = PartialHttpRequest.FromBytes(message.MessageBytes);
                }
                catch(Exception e)
                {
                    server.LogDebug(e.Message + "\n" + e.StackTrace);
                    return false;
                }
                if (request.Method == HttpMessage.HttpMethods.Get && request.Headers.ContainsHeader(HttpMessage.GeneralHeaders.Connection) &&
                    request.Headers[HttpMessage.GeneralHeaders.Connection].ToLower() == "upgrade" && request.Headers.ContainsHeader(WebsocketProtocolVersionKey) &&
                    request.Headers[WebsocketProtocolVersionKey] == "13")
                {
                    if (!moduleDictionary.ContainsKey(request.RequestURI))
                    {
                        QueueMessage(client, new HttpResponse { StatusCode = HttpMessage.StatusCodes.NotFound, BodyString = "URI not found" }.GetPackedResponse(true));
                    }
                    (var module, var routeEnabler) = moduleDictionary[request.RequestURI];
                    var routeEnablerResponse = routeEnabler.Invoke(server, request.ToRequest(), client);
                    if(routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseIgnore)
                    {
                        return false;
                    }
                    else if(routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseError)
                    {
                        QueueMessage(client, (routeEnablerResponse as RouteEnablerResponse.RouteEnablerResponseError).Response.GetPackedResponse(true));
                        return true;
                    }
                    /*
                     * The RouteEnabler accepted the request.
                     * Prepare the handshake string.
                     */
                    string base64Key = request.Headers[WebsocketHeaderKey];
                    base64Key = base64Key.Trim();
                    string handshakeKey = base64Key + GlobalUniqueIdentifier;
                    string returnBase64Key = Convert.ToBase64String(sha1Provider.ComputeHash(Encoding.UTF8.GetBytes(handshakeKey)));

                    /*
                     * Prepare the response.
                     */
                    HttpResponse response = new HttpResponse();
                    response.StatusCode = HttpMessage.StatusCodes.SwitchingProtocols;
                    response.Headers[HttpMessage.GeneralHeaders.Upgrade] = "websocket";
                    response.Headers[HttpMessage.GeneralHeaders.Connection] = "Upgrade";
                    response.Headers[WebsocketHeaderAcceptKey] = returnBase64Key;
                    server.QueueMessage(client, response.GetPackedResponse(true));
                    webSockets[client] = SocketState.Established;
                    server.LogDebug("Websocket initialized " + client.TcpClient.Client.RemoteEndPoint.ToString());
                    routingTable[client] = module;
                    module.CallConnectionInitialized(server, this, client);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (webSockets[client] == SocketState.Established)
            {
                WebsocketMessage receivedMessage = null;
                try
                {
                    receivedMessage = new WebsocketMessage(message.MessageBytes);
                }
                catch(Exception e)
                {
                    server.LogDebug(e.Message + "\n" + e.StackTrace);
                    return false;
                }

                if (receivedMessage.Opcode == WebsocketMessage.Opcodes.Close)
                {
                    client.ToBeRemoved = true;
                    while (webSockets.ContainsKey(client))
                    {
                        webSockets.TryRemove(client, out SocketState _);
                    }
                    WebsocketMessage closeFrame = new WebsocketMessage();
                    closeFrame.Opcode = WebsocketMessage.Opcodes.Close;
                    QueueMessage(client, closeFrame);
                    return true;
                }
                else
                {
                    try 
                    {
                        routingTable[client].CallHandleReceivedMessage(server, this, client, receivedMessage);
                        return true;
                    }
                    catch(Exception e)
                    {
                        server.LogDebug(e.Message + "\n" + e.StackTrace);
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
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
            foreach((var module, var _) in moduleDictionary.Values)
            {
                module.Tick(server, this);
            }
            while (messageQueue.Count > 0)
            {
                if (messageQueue.TryDequeue(out Tuple<ClientData, WebsocketMessage> tuple))
                {
                    server.QueueMessage(tuple.Item1, tuple.Item2.GetMessageBytes());
                    if (tuple.Item2.Opcode == WebsocketMessage.Opcodes.Close)
                    {
                        if (routingTable.ContainsKey(tuple.Item1))
                        {
                            routingTable[tuple.Item1].CallConnectionClosed(server, this, tuple.Item1);
                        }
                        tuple.Item1.ToBeRemoved = true;
                    }
                }
            }
        }
        #endregion
    }
}
