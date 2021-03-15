using MTSC.Common.Http;
using MTSC.Common.WebSockets;
using MTSC.Common.WebSockets.RoutingModules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MTSC.ServerSide.Handlers
{
    public class WebsocketRoutingHandler : IHandler, IRunOnStartup
    {
        private const string WebsocketHeaderAcceptKey = "Sec-WebSocket-Accept";
        private const string WebsocketHeaderKey = "Sec-WebSocket-Key";
        private const string WebsocketProtocolKey = "Sec-WebSocket-Protocol";
        private const string WebsocketProtocolVersionKey = "Sec-WebSocket-Version";
        private const string GlobalUniqueIdentifier = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private static readonly Func<Server, HttpRequest, ClientData, RouteEnablerResponse> alwaysEnabled = (server, message, client) => RouteEnablerResponse.Accept;
        private static readonly SHA1 sha1Provider = SHA1.Create();
        private static readonly RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
        public enum SocketState
        {
            Initial,
            Handshaking,
            Established,
            Closed
        }
        #region Fields
        private readonly Dictionary<string, (Type, Func<Server, HttpRequest, ClientData, RouteEnablerResponse>)> moduleDictionary =
            new Dictionary<string, (Type, Func<Server, HttpRequest, ClientData, RouteEnablerResponse>)>();
        private readonly ConcurrentQueue<Tuple<ClientData, WebsocketMessage>> messageQueue = new ConcurrentQueue<Tuple<ClientData, WebsocketMessage>>();
        #endregion
        #region Public Methods
        public WebsocketRoutingHandler AddRoute<T>(string uri)
            where T : WebsocketRouteBase
        {
            this.RegisterRoute(uri, typeof(T), alwaysEnabled);   
            return this;
        }
        public WebsocketRoutingHandler AddRoute<T>(
            string uri,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
            where T : WebsocketRouteBase
        {
            this.RegisterRoute(uri, typeof(T), routeEnabler);
            return this;
        }
        public WebsocketRoutingHandler AddRoute(string uri, Type routeType)
        {
            this.RegisterRoute(uri, routeType, alwaysEnabled);
            return this;
        }
        public WebsocketRoutingHandler AddRoute(
            string uri,
            Type routeType,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
        {
            this.RegisterRoute(uri, routeType, routeEnabler);
            return this;
        }
        public WebsocketRoutingHandler RemoveRoute(string uri)
        {
            this.moduleDictionary.Remove(uri);
            return this;
        }

        public void QueueMessage(ClientData client, byte[] message, WebsocketMessage.Opcodes opcode = WebsocketMessage.Opcodes.Text)
        {
            WebsocketMessage sendMessage = new WebsocketMessage
            {
                Data = message,
                FIN = true,
                Masked = false
            };
            rng.GetBytes(sendMessage.Mask);
            sendMessage.Opcode = opcode;
            this.messageQueue.Enqueue(new Tuple<ClientData, WebsocketMessage>(client, sendMessage));
        }

        public void QueueMessage(ClientData client, WebsocketMessage message)
        {
            this.messageQueue.Enqueue(new Tuple<ClientData, WebsocketMessage>(client, message));
        }

        public void CloseConnection(ClientData client)
        {
            WebsocketMessage websocketMessage = new WebsocketMessage
            {
                FIN = true,
                Opcode = WebsocketMessage.Opcodes.Close,
                Masked = false
            };
            this.QueueMessage(client, websocketMessage);
        }
        #endregion
        #region Handler Implementation
        void IHandler.ClientRemoved(Server server, ClientData client)
        {
            if (client.Resources.TryGetResource(out WebsocketRouteBase route))
            {
                route.CallConnectionClosed();
            }
        }

        bool IHandler.HandleClient(Server server, ClientData client)
        {
            client.Resources.SetResource(SocketState.Initial);
            return false;
        }

        bool IHandler.HandleReceivedMessage(Server server, ClientData client, Message message)
        {
            var socketState = client.Resources.GetResource<SocketState>();
            if (socketState == SocketState.Initial)
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
                    if (!this.moduleDictionary.ContainsKey(request.RequestURI))
                    {
                        this.QueueMessage(client, new HttpResponse { StatusCode = HttpMessage.StatusCodes.NotFound, BodyString = "URI not found" }.GetPackedResponse(true));
                    }

                    (var moduleType, var routeEnabler) = moduleDictionary[request.RequestURI];
                    var routeEnablerResponse = routeEnabler.Invoke(server, request.ToRequest(), client);
                    if(routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseIgnore)
                    {
                        return false;
                    }
                    else if(routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseError)
                    {
                        this.QueueMessage(client, (routeEnablerResponse as RouteEnablerResponse.RouteEnablerResponseError).Response.GetPackedResponse(true));
                        return true;
                    }

                    /*
                     * The RouteEnabler accepted the request.
                     * Prepare the handshake string.
                     */
                    var base64Key = request.Headers[WebsocketHeaderKey];
                    base64Key = base64Key.Trim();
                    var handshakeKey = base64Key + GlobalUniqueIdentifier;
                    var returnBase64Key = Convert.ToBase64String(sha1Provider.ComputeHash(Encoding.UTF8.GetBytes(handshakeKey)));

                    /*
                     * Prepare the response.
                     */
                    var response = new HttpResponse
                    {
                        StatusCode = HttpMessage.StatusCodes.SwitchingProtocols
                    };
                    response.Headers[HttpMessage.GeneralHeaders.Upgrade] = "websocket";
                    response.Headers[HttpMessage.GeneralHeaders.Connection] = "Upgrade";
                    response.Headers[WebsocketHeaderAcceptKey] = returnBase64Key;
                    server.QueueMessage(client, response.GetPackedResponse(false));
                    client.Resources.SetResource(SocketState.Established);
                    server.LogDebug("Websocket initialized " + client.TcpClient.Client.RemoteEndPoint.ToString());
                    /*
                     * Create and assign route module to client.
                     */
                    if (server.ServiceManager.GetService(moduleType) is not WebsocketRouteBase module)
                    {
                        throw new InvalidOperationException($"Unexpected error during websocket module initialization. {moduleType.FullName} is not of type {typeof(WebsocketRouteBase).FullName}");
                    }

                    (module as ISetWebsocketContext).SetClient(client);
                    (module as ISetWebsocketContext).SetHandler(this);
                    (module as ISetWebsocketContext).SetServer(server);
                    client.Resources.SetResource(module);
                    module.CallConnectionInitialized();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (socketState == SocketState.Established)
            {
                WebsocketMessage receivedMessage;
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
                    this.QueueMessage(client, new byte[0], WebsocketMessage.Opcodes.Close);
                    return true;
                }
                else if (receivedMessage.Opcode == WebsocketMessage.Opcodes.Ping)
                {
                    this.QueueMessage(client, new byte[0] ,WebsocketMessage.Opcodes.Pong);
                    return true;
                }
                else
                {
                    try 
                    {
                        client.Resources.GetResource<WebsocketRouteBase>().CallHandleReceivedMessage(receivedMessage);
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
            foreach(var client in server.Clients)
            {
                if (client.Resources.TryGetResource<WebsocketRouteBase>(out var route))
                {
                    route.Tick();
                }
            }

            while (this.messageQueue.Count > 0)
            {
                if (this.messageQueue.TryDequeue(out Tuple<ClientData, WebsocketMessage> tuple))
                {
                    server.QueueMessage(tuple.Item1, tuple.Item2.GetMessageBytes());
                    if (tuple.Item2.Opcode == WebsocketMessage.Opcodes.Close)
                    {
                        if (tuple.Item1.Resources.TryGetResource<WebsocketRouteBase>(out var route))
                        {
                            route.CallConnectionClosed();
                        }
                        tuple.Item1.ToBeRemoved = true;
                    }
                }
            }
        }

        void IRunOnStartup.OnStartup(Server server)
        {
            foreach((var routeType, _) in this.moduleDictionary.Values)
            {
                server.ServiceManager.RegisterTransient(routeType, routeType);
            }
        }
        #endregion

        private void RegisterRoute(string uri, Type moduleType, Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
        {
            if (!typeof(WebsocketRouteBase).IsAssignableFrom(moduleType))
            {
                throw new InvalidOperationException($"{moduleType.FullName} must be of type {typeof(WebsocketRouteBase).FullName}");
            }

            this.moduleDictionary.Add(uri, (moduleType, routeEnabler));
        }
    }
}
