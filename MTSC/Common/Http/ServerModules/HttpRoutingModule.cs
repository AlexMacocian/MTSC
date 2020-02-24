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
        private static Func<Server.Server, HttpRequest, ClientData, RouteEnablerResponse> alwaysEnabled = (server, request, client) => RouteEnablerResponse.Accept;

        private Dictionary<HttpMethods, Dictionary<string, (HttpRouteBase, 
            Func<Server.Server, HttpRequest, ClientData, RouteEnablerResponse>)>> moduleDictionary = 
            new Dictionary<HttpMethods, Dictionary<string, (HttpRouteBase, 
                Func<Server.Server, HttpRequest, ClientData, RouteEnablerResponse>)>>();

        public HttpRoutingModule()
        {
            foreach (HttpMethods method in (HttpMethods[])Enum.GetValues(typeof(HttpMethods)))
            {
                moduleDictionary[method] = new Dictionary<string, (HttpRouteBase, 
                    Func<Server.Server, HttpRequest, ClientData, RouteEnablerResponse>)>();
            }
        }

        public HttpRoutingModule AddRoute(
            HttpMethods method,
            string uri,
            HttpRouteBase routeModule)
        {
            moduleDictionary[method][uri] = (routeModule, alwaysEnabled);
            return this;
        }

        public HttpRoutingModule AddRoute(
            HttpMethods method,
            string uri,
            HttpRouteBase routeModule,
            Func<Server.Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
        {
            moduleDictionary[method][uri] = (routeModule, routeEnabler);
            return this;
        }

        public HttpRoutingModule RemoveRoute(
            HttpMethods method,
            string uri)
        {
            moduleDictionary[method].Remove(uri);
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
                var routeEnablerResponse = routeEnabler.Invoke(server, request, client);
                if (routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseAccept)
                {
                    try
                    {
                        response = module.CallHandleRequest(request, client, server);
                        //response = module.HandleRequest(requestTemplate.Invoke(request), client, server);
                    }
                    catch(Exception e)
                    {
                        server.LogDebug("Exception: " + e.Message);
                        server.LogDebug("Stacktrace: " + e.StackTrace);
                        response = new HttpResponse() { StatusCode = StatusCodes.InternalServerError };
                    }
                    return true;
                }
                else if(routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseIgnore)
                {
                    return false;
                }
                else if(routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseError)
                {
                    response = (routeEnablerResponse as RouteEnablerResponse.RouteEnablerResponseError).Response;
                    return true;
                }
            }
            return false;
        }

        void IHttpModule.Tick(Server.Server server, HttpHandler handler) { }
    }
}
