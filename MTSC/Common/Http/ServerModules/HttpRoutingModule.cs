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
        private Dictionary<HttpMethods, Dictionary<string, (IHttpRoute, Func<Server.Server, HttpRequest, ClientData, bool>)>> moduleDictionary = 
            new Dictionary<HttpMethods, Dictionary<string, (IHttpRoute, Func<Server.Server, HttpRequest, ClientData, bool>)>>();

        public HttpRoutingModule()
        {
            foreach (HttpMethods method in (HttpMethods[])Enum.GetValues(typeof(HttpMethods)))
            {
                moduleDictionary[method] = new Dictionary<string, (IHttpRoute, Func<Server.Server, HttpRequest, ClientData, bool>)>();
            }
        }

        public HttpRoutingModule AddRoute(HttpMethods method, string uri, IHttpRoute routeModule, Func<Server.Server, HttpRequest, ClientData, bool> routeEnabler = null)
        {
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
                if(routeEnabler == null || routeEnabler.Invoke(server, request, client))
                {
                    try
                    {
                        response = module.HandleRequest(request, client, server);
                    }
                    catch(Exception e)
                    {
                        server.LogDebug("Exception: " + e.Message);
                        server.LogDebug("Stacktrace: " + e.StackTrace);
                        response = new HttpResponse() { StatusCode = StatusCodes.InternalServerError };
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
