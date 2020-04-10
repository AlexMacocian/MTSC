﻿using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.ServerSide.Handlers
{
    public sealed class HttpRoutingHandler : IHandler
    {
        private static Func<Server, HttpRequest, ClientData, RouteEnablerResponse> alwaysEnabled = (server, request, client) => RouteEnablerResponse.Accept;

        private ConcurrentQueue<Tuple<ClientData, HttpResponse>> messageOutQueue = new ConcurrentQueue<Tuple<ClientData, HttpResponse>>();

        private Dictionary<HttpMethods, Dictionary<string, (HttpRouteBase,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse>)>> moduleDictionary =
            new Dictionary<HttpMethods, Dictionary<string, (HttpRouteBase,
                Func<Server, HttpRequest, ClientData, RouteEnablerResponse>)>>();

        public TimeSpan FragmentsExpirationTime { get; set; } = TimeSpan.FromSeconds(15);
        public double MaximumRequestSize { get; set; } = double.MaxValue;

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
            /*
             * If a fragmented request exists, add the new messages to the body.
             * Else, parse the messages into a partial request. If this causes an exception, let it throw out of the handler,
             * cause the handler needs at least valid headers to work.
             */
            PartialHttpRequest request = null;
            if (client.Resources.TryGetResource<FragmentedMessage>(out var fragmentedMessage)) 
            {
                var bytesToBeAdded = message.MessageBytes.TrimTrailingNullBytes();
                if(fragmentedMessage.PartialRequest.HeaderByteCount + 
                    fragmentedMessage.PartialRequest.Body.Length + 
                    message.MessageLength > this.MaximumRequestSize)
                {
                    QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request exceeded [{MaximumRequestSize}] bytes!" });
                    client.ResetAffinityIfMe(this);
                    client.Resources.RemoveResource<FragmentedMessage>();
                    client.Resources.RemoveResourceIfExists<RequestMapping>();
                }
                fragmentedMessage.AddToMessage(bytesToBeAdded);
                request = fragmentedMessage.PartialRequest;
            }
            else
            {
                if (message.MessageLength > this.MaximumRequestSize)
                {
                    QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request exceeded [{MaximumRequestSize}] bytes!" });
                    return false;
                }

                request = PartialHttpRequest.FromBytes(message.MessageBytes.TrimTrailingNullBytes());
                if (!request.Complete)
                {
                    client.Resources.SetResource(new FragmentedMessage { LastReceived = DateTime.Now, PartialRequest = request });
                    client.SetAffinity(this);
                    if (request.Headers.ContainsHeader(RequestHeaders.Expect) &&
                        request.Headers[RequestHeaders.Expect].Equals("100-continue", StringComparison.OrdinalIgnoreCase))
                    {
                        server.LogDebug("Returning 100-Continue");
                        var contResponse = new HttpResponse { StatusCode = HttpMessage.StatusCodes.Continue };
                        contResponse.Headers[HttpMessage.GeneralHeaders.Connection] = "keep-alive";
                        QueueResponse(client, contResponse);
                    }
                }
            }

            /*
             * Once headers are loaded, check if mapping exists. If mapping exists between request and module,
             * verify that the request is complete and send it to module.
             * Otherwise, if the request is complete, send it to the mapped module. If the request is not complete,
             * handle it and return.
             */

            if (client.Resources.TryGetResource<RequestMapping>(out var mapping))
            {
                if (request.Complete)
                {
                    client.Resources.RemoveResource<RequestMapping>();
                    client.Resources.RemoveResource<FragmentedMessage>();
                    client.ResetAffinityIfMe(this);
                    HandleCompleteRequest(client, server, request.ToRequest(), mapping.MappedModule, mapping.RouteEnabler);
                    return true;
                }
                else
                {
                    HandleIncompleteRequest(client, server, client.Resources.GetResource<FragmentedMessage>());
                    return true;
                }
            }
            else
            {
                if (moduleDictionary[request.Method].ContainsKey(request.RequestURI))
                {
                    (var module, var routeEnabler) = moduleDictionary[request.Method][request.RequestURI];
                    if (request.Complete)
                    {
                        var httpRequest = request.ToRequest();
                        client.ResetAffinityIfMe(this);
                        return HandleCompleteRequest(client, server, httpRequest, module, routeEnabler);
                    }
                    else
                    {
                        client.Resources.SetResource(new RequestMapping { MappedModule = module, RouteEnabler = routeEnabler });
                        return true;
                    }
                }
                else
                {
                    client.ResetAffinityIfMe(this);
                    client.Resources.RemoveResource<FragmentedMessage>();
                    return false;
                }
            }
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
            foreach (var client in server.Clients)
            {
                if (client.Resources.TryGetResource<FragmentedMessage>(out var fragmentedMessage)) 
                {
                    if ((DateTime.Now - fragmentedMessage.LastReceived) > FragmentsExpirationTime)
                    {
                        client.Resources.RemoveResource<FragmentedMessage>();
                        QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request timed out in [{FragmentsExpirationTime.TotalMilliseconds}] ms!" });
                    }
                }
            }
        }

        private void HandleIncompleteRequest(ClientData client, Server server, FragmentedMessage fragmentedMessage)
        {
            fragmentedMessage.LastReceived = DateTime.Now;
            server.LogDebug("Incomplete request received!");
        }
        
        private bool HandleCompleteRequest(
            ClientData client, 
            Server server, 
            HttpRequest request, 
            HttpRouteBase module,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
        {
            var routeEnablerResponse = routeEnabler.Invoke(server, request, client);
            if (routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseAccept)
            {
                try
                {
                    module.CallHandleRequest(request, client, server).ContinueWith((task) => { QueueResponse(client, task.Result); });
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
            else
            {
                throw new InvalidOperationException($"RouteEnablerResponse should be one of the types {typeof(RouteEnablerResponse.RouteEnablerResponseAccept)}, {typeof(RouteEnablerResponse.RouteEnablerResponseError)} or {typeof(RouteEnablerResponse.RouteEnablerResponseIgnore)}!");
            }
        }

        private class FragmentedMessage
        {
            public PartialHttpRequest PartialRequest { get; set; }

            public DateTime LastReceived { get; set; } = DateTime.Now;

            public void AddToMessage(byte[] bytes)
            {
                this.PartialRequest.AddToBody(bytes);
            }
        }

        private class RequestMapping
        {
            public HttpRouteBase MappedModule { get; set; }
            public Func<Server, HttpRequest, ClientData, RouteEnablerResponse> RouteEnabler { get; set; }
        }
    }
}
