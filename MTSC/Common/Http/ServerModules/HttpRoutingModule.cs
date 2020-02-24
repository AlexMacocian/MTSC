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
        private static Func<HttpRequest, HttpRequest> noTemplate = (request) => request;

        private Dictionary<HttpMethods, Dictionary<string, (IHttpRoute<ITemplatedHttpRequest>, 
            Func<Server.Server, HttpRequest, ClientData, RouteEnablerResponse>, Func<HttpRequest, ITemplatedHttpRequest> template)>> moduleDictionary = 
            new Dictionary<HttpMethods, Dictionary<string, (IHttpRoute<ITemplatedHttpRequest>, 
                Func<Server.Server, HttpRequest, ClientData, RouteEnablerResponse>, Func<HttpRequest, ITemplatedHttpRequest>)>>();

        public HttpRoutingModule()
        {
            foreach (HttpMethods method in (HttpMethods[])Enum.GetValues(typeof(HttpMethods)))
            {
                moduleDictionary[method] = new Dictionary<string, (IHttpRoute<ITemplatedHttpRequest>, 
                    Func<Server.Server, HttpRequest, ClientData, RouteEnablerResponse>, Func<HttpRequest, ITemplatedHttpRequest> template)>();
            }
        }

        public HttpRoutingModule AddRoute(
            HttpMethods method,
            string uri,
            IHttpRoute<ITemplatedHttpRequest> routeModule)
        {
            moduleDictionary[method][uri] = (routeModule, alwaysEnabled, noTemplate);
            return this;
        }

        public HttpRoutingModule AddRoute(
            HttpMethods method,
            string uri,
            IHttpRoute<ITemplatedHttpRequest> routeModule,
            Func<Server.Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
        {
            moduleDictionary[method][uri] = (routeModule, routeEnabler, noTemplate);
            return this;
        }

        public HttpRoutingModule AddRoute(
            HttpMethods method,
            string uri,
            IHttpRoute<ITemplatedHttpRequest> routeModule,
            Func<HttpRequest, ITemplatedHttpRequest> requestTemplate)
        {
            moduleDictionary[method][uri] = (routeModule, alwaysEnabled, requestTemplate);
            return this;
        }

        public HttpRoutingModule AddRoute(
            HttpMethods method,
            string uri,
            IHttpRoute<ITemplatedHttpRequest> routeModule,
            Func<Server.Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler,
            Func<HttpRequest, ITemplatedHttpRequest> requestTemplate)
        {
            moduleDictionary[method][uri] = (routeModule, routeEnabler, requestTemplate);
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
                (var module, var routeEnabler, var requestTemplate) = moduleDictionary[request.Method][request.RequestURI];
                var routeEnablerResponse = routeEnabler.Invoke(server, request, client);
                if (routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseAccept)
                {
                    try
                    {
                        response = module.HandleRequest(requestTemplate.Invoke(request), client, server);
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
