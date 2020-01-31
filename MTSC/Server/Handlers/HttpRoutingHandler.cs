using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.Server.Handlers
{
    public class HttpRoutingHandler : IHandler
    {
        ConcurrentDictionary<ClientData, HttpRequest> fragmentedMessages = new ConcurrentDictionary<ClientData, HttpRequest>();
        private Dictionary<HttpMethods, Dictionary<string, (IHttpRoute, Func<HttpRequest, ClientData, bool>)>> moduleDictionary = 
            new Dictionary<HttpMethods, Dictionary<string, (IHttpRoute, Func<HttpRequest, ClientData, bool>)>>();

        public HttpRoutingHandler()
        {
            foreach (HttpMethods method in (HttpMethods[])Enum.GetValues(typeof(HttpMethods)))
            {
                moduleDictionary[method] = new Dictionary<string, (IHttpRoute, Func<HttpRequest, ClientData, bool>)>();
            }
        }

        public HttpRoutingHandler AddRoute(HttpMethods method, string uri, IHttpRoute routeModule, Func<HttpRequest, ClientData, bool> routeEnabler = null)
        {
            if(routeEnabler == null)
            {
                routeEnabler = (request, client) => { return true; };
            }
            moduleDictionary[method][uri] = (routeModule, routeEnabler);
            return this;
        }

        void IHandler.ClientRemoved(Server server, ClientData client) { }

        bool IHandler.HandleClient(Server server, ClientData client) => false;

        bool IHandler.HandleReceivedMessage(Server server, ClientData client, Message message)
        {
            HttpRequest request;
            /*
             * If there's an existing fragmented request, get it from the storage.
             */
            if (fragmentedMessages.ContainsKey(client))
            {
                request = fragmentedMessages[client];
                request.AddToBody(message.MessageBytes);
            }
            else
            {
                request = HttpRequest.FromBytes(message.MessageBytes);
            }

            /*
             * If the server hasn't received all the bytes specified by the request, 
             * add the request to storage and wait for the rest of bytes to be received.
             */

            if (request.Headers.ContainsHeader(HttpMessage.EntityHeaders.ContentLength) &&
                    request.Body.Length < int.Parse(request.Headers[HttpMessage.EntityHeaders.ContentLength]))
            {
                fragmentedMessages[client] = request;
                var continueResponse = new HttpResponse();
                continueResponse.StatusCode = HttpMessage.StatusCodes.Continue;
                server.QueueMessage(client, continueResponse.GetPackedResponse(true));
                return true;
            }
            else
            {
                fragmentedMessages.TryRemove(client, out _);
            }

            HttpResponse response = new HttpResponse();
            if (request.Headers.ContainsHeader(HttpMessage.GeneralHeaders.Connection) &&
                request.Headers[HttpMessage.GeneralHeaders.Connection].ToLower() == "close")
            {
                response.Headers[HttpMessage.GeneralHeaders.Connection] = "close";
                client.ToBeRemoved = true;
            }
            else
            {
                response.Headers[HttpMessage.GeneralHeaders.Connection] = "keep-alive";
            }

            /*
             * Now find if a routing module exists. If not let other handlers try and handle the message.
             */
            if (moduleDictionary[request.Method].ContainsKey(request.RequestURI))
            {
                (var module, var routeEnabler) = moduleDictionary[request.Method][request.RequestURI];
                if(routeEnabler.Invoke(request, client))
                {
                    try
                    {
                        server.QueueMessage(client, module.HandleRequest(request, client, server).GetPackedResponse(true));
                    }
                    catch(Exception e)
                    {
                        server.LogDebug("Exception: " + e.Message);
                        server.LogDebug("Stacktrace: " + e.StackTrace);
                        server.QueueMessage(client, new HttpResponse() { StatusCode = StatusCodes.InternalServerError }.GetPackedResponse(true));
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        bool IHandler.HandleSendMessage(Server server, ClientData client, ref Message message) => false;

        bool IHandler.PreHandleReceivedMessage(Server server, ClientData client, ref Message message) => false;

        void IHandler.Tick(Server server) { }
    }
}
