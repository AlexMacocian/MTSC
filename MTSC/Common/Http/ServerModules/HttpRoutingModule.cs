using MTSC.Common.Http.RoutingModules;
using MTSC.Server;
using MTSC.Server.Handlers;
using System;
using System.Collections.Generic;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.Common.Http.ServerModules
{
    public class HttpRoutingModule : IHttpModule
    {
        private Dictionary<HttpMethods, Dictionary<string, (IHttpRoute, Func<HttpRequest, ClientData, bool>)>> moduleDictionary = 
            new Dictionary<HttpMethods, Dictionary<string, (IHttpRoute, Func<HttpRequest, ClientData, bool>)>>();

        public HttpRoutingModule()
        {
            foreach (HttpMethods method in (HttpMethods[])Enum.GetValues(typeof(HttpMethods)))
            {
                moduleDictionary[method] = new Dictionary<string, (IHttpRoute, Func<HttpRequest, ClientData, bool>)>();
            }
        }

        public HttpRoutingModule AddRoute(HttpMethods method, string uri, IHttpRoute routeModule, Func<HttpRequest, ClientData, bool> routeEnabler = null)
        {
            if(routeEnabler == null)
            {
                routeEnabler = (request, client) => { return true; };
            }
            moduleDictionary[method][uri] = (routeModule, routeEnabler);
            return this;
        }

        bool IHttpModule.HandleRequest(Server.Server server, HttpHandler handler, ClientData client, HttpRequest request, ref HttpResponse response)
        {
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

        void IHttpModule.Tick(Server.Server server, HttpHandler handler) { }
    }
}
