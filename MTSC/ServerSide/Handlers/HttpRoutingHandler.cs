using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.ServerSide.Handlers
{
    public sealed class HttpRoutingHandler : IHandler
    {
        private static Func<Server, HttpRequest, ClientData, RouteEnablerResponse> alwaysEnabled = (server, request, client) => RouteEnablerResponse.Accept;

        private List<ClientData> removeFragmentsList = new List<ClientData>();
        private ConcurrentQueue<Tuple<ClientData, HttpResponse>> messageOutQueue = new ConcurrentQueue<Tuple<ClientData, HttpResponse>>();
        private ConcurrentDictionary<ClientData, (byte[], DateTime)> fragmentedMessages = new ConcurrentDictionary<ClientData, (byte[], DateTime)>();

        private Dictionary<HttpMethods, Dictionary<string, (HttpRouteBase,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse>)>> moduleDictionary =
            new Dictionary<HttpMethods, Dictionary<string, (HttpRouteBase,
                Func<Server, HttpRequest, ClientData, RouteEnablerResponse>)>>();

        public TimeSpan FragmentsExpirationTime { get; set; } = TimeSpan.FromSeconds(15);
        public double MaximumRequestSize { get; set; } = 15000;

        public HttpRoutingHandler()
        {
            foreach (HttpMethods method in (HttpMethods[])Enum.GetValues(typeof(HttpMethods)))
            {
                moduleDictionary[method] = new Dictionary<string, (HttpRouteBase,
                    Func<ServerSide.Server, HttpRequest, ClientData, RouteEnablerResponse>)>();
            }
        }

        public HttpRoutingHandler AddRoute(
            HttpMethods method,
            string uri,
            HttpRouteBase routeModule)
        {
            moduleDictionary[method][uri] = (routeModule, alwaysEnabled);
            return this;
        }
        public HttpRoutingHandler AddRoute(
            HttpMethods method,
            string uri,
            HttpRouteBase routeModule,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
        {
            moduleDictionary[method][uri] = (routeModule, routeEnabler);
            return this;
        }
        public HttpRoutingHandler RemoveRoute(
            HttpMethods method,
            string uri)
        {
            moduleDictionary[method].Remove(uri);
            return this;
        }

        public HttpRoutingHandler WithMaximumSize(double size)
        {
            this.MaximumRequestSize = size;
            return this;
        }
        /// <summary>
        /// The amount of time fragments are kept in the buffer before being discarded.
        /// </summary>
        /// <param name="duration">Time until fragments expire.</param>
        /// <returns>This handler object.</returns>
        public HttpRoutingHandler WithFragmentsExpirationTime(TimeSpan duration)
        {
            this.FragmentsExpirationTime = duration;
            return this;
        }
        /// <summary>
        /// Send a response back to the client.
        /// </summary>
        /// <param name="response">Message containing the response.</param>
        public void QueueResponse(ClientData client, HttpResponse response)
        {
            messageOutQueue.Enqueue(new Tuple<ClientData, HttpResponse>(client, response));
        }

        void IHandler.ClientRemoved(Server server, ClientData client) { }

        bool IHandler.HandleClient(Server server, ClientData client) => false;

        bool IHandler.HandleReceivedMessage(Server server, ClientData client, Message message)
        {
            // Parse the request. If the message is incomplete, return 100 and queue the message to be parsed later.
            HttpRequest request = null;
            byte[] messageBytes = null;
            try
            {
                if (fragmentedMessages.ContainsKey(client))
                {
                    byte[] previousBytes = fragmentedMessages[client].Item1;
                    if (previousBytes.Length + message.MessageBytes.Length > MaximumRequestSize)
                    {
                        // Discard the message if it is too big
                        server.LogDebug($"Discarded message. Message size [{previousBytes.Length + message.MessageBytes.Length}] > [{MaximumRequestSize}]");
                        fragmentedMessages.TryRemove(client, out _);
                        return false;
                    }
                    byte[] repackagingBuffer = new byte[previousBytes.Length + message.MessageBytes.Length];
                    Array.Copy(previousBytes, 0, repackagingBuffer, 0, previousBytes.Length);
                    Array.Copy(message.MessageBytes, 0, repackagingBuffer, previousBytes.Length, message.MessageBytes.Length);
                    messageBytes = repackagingBuffer;
                }
                else
                {
                    if (message.MessageBytes.Length > MaximumRequestSize)
                    {
                        // Discard the message if it is too big
                        server.LogDebug($"Discarded message. Message size [{message.MessageBytes.Length}] > [{MaximumRequestSize}]");
                        return false;
                    }
                    messageBytes = message.MessageBytes;
                }
                messageBytes = messageBytes.TrimTrailingNullBytes();
                var partialRequest = PartialHttpRequest.FromBytes(messageBytes);
                if (partialRequest.Complete)
                    request = partialRequest.ToRequest();
                else
                {
                    HandleIncompleteRequest(client, server, messageBytes, partialRequest);
                    return true;
                }
            }
            catch (Exception ex) when (
                ex is IncompleteHeaderKeyException ||
                ex is IncompleteHeaderValueException ||
                ex is IncompleteHttpVersionException ||
                ex is IncompleteMethodException ||
                ex is IncompleteRequestBodyException ||
                ex is IncompleteRequestQueryException ||
                ex is IncompleteRequestURIException ||
                ex is IncompleteRequestException ||
                ex is InvalidPostFormException)
            {
                server.LogDebug(ex.Message);
                server.LogDebug(ex.StackTrace);
                HandleIncompleteRequest(client, server, messageBytes);
                return true;
            }
            catch (Exception e)
            {
                throw e;
            }

            // The message has been parsed. If there was a cache for the current message, remove it.
            if (fragmentedMessages.ContainsKey(client))
            {
                fragmentedMessages.TryRemove(client, out _);
            }

            /*
             * Now find if a routing module exists. If not let other handlers try and handle the message.
             */
            if (moduleDictionary[request.Method].ContainsKey(request.RequestURI))
            {
                (var module, var routeEnabler) = moduleDictionary[request.Method][request.RequestURI];
                var routeEnablerResponse = routeEnabler.Invoke(server, request, client);
                if (routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseAccept)
                {
                    try
                    {
                        module.CallHandleRequest(request, client, server).ContinueWith((task) => { QueueResponse(client, task.Result); });
                        //response = module.HandleRequest(requestTemplate.Invoke(request), client, server);
                    }
                    catch (Exception e)
                    {
                        server.LogDebug("Exception: " + e.Message);
                        server.LogDebug("Stacktrace: " + e.StackTrace);
                        QueueResponse(client, new HttpResponse() { StatusCode = StatusCodes.InternalServerError });
                    }
                    return true;
                }
                else if (routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseIgnore)
                {
                    return false;
                }
                else if (routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseError)
                {
                    QueueResponse(client, (routeEnablerResponse as RouteEnablerResponse.RouteEnablerResponseError).Response);
                    return true;
                }
            }
            return false;
        }

        bool IHandler.HandleSendMessage(Server server, ClientData client, ref Message message) => false;

        bool IHandler.PreHandleReceivedMessage(Server server, ClientData client, ref Message message) => false;

        void IHandler.Tick(Server server)
        {
            while (messageOutQueue.Count > 0)
            {
                if (messageOutQueue.TryDequeue(out Tuple<ClientData, HttpResponse> tuple))
                {
                    server.QueueMessage(tuple.Item1, tuple.Item2.GetPackedResponse(true));
                }
            }
            removeFragmentsList.Clear();
            foreach (var kvp in fragmentedMessages)
            {
                if ((DateTime.Now - kvp.Value.Item2) > FragmentsExpirationTime)
                {
                    removeFragmentsList.Add(kvp.Key);
                }
            }
            foreach (var key in removeFragmentsList)
            {
                fragmentedMessages.TryRemove(key, out _);
            }
        }

        private void HandleIncompleteRequest(ClientData client, Server server, byte[] messageBytes, PartialHttpRequest partialRequest = null)
        {
            fragmentedMessages[client] = (messageBytes, DateTime.Now);
            server.LogDebug("Incomplete request received!");
            if (partialRequest != null && partialRequest.Headers.ContainsHeader(HttpMessage.RequestHeaders.Expect) &&
                partialRequest.Headers[HttpMessage.RequestHeaders.Expect].Equals("100-continue", StringComparison.OrdinalIgnoreCase))
            {
                server.LogDebug("Returning 100-Continue");
                var contResponse = new HttpResponse { StatusCode = HttpMessage.StatusCodes.Continue };
                contResponse.Headers[HttpMessage.GeneralHeaders.Connection] = "keep-alive";
                QueueResponse(client, contResponse);
            }
        }
    }
}
