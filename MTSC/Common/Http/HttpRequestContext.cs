using MTSC.Common.Http.RoutingModules;
using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;

namespace MTSC.Common.Http
{
    public sealed class HttpRequestContext
    {
        public ClientData ClientData { get; }
        public HttpRequest Request { get; }
        public HttpRouteBase Route { get; }
        public HttpRoutingHandler RoutingHandler { get; }

        internal HttpRequestContext(
            ClientData clientData,
            HttpRequest httpRequest,
            HttpRouteBase httpRouteBase,
            HttpRoutingHandler httpRoutingHandler)
        {
            this.ClientData = clientData ?? throw new ArgumentNullException(nameof(clientData));
            this.Request = httpRequest ?? throw new ArgumentNullException(nameof(httpRequest));
            this.Route = httpRouteBase ?? throw new ArgumentNullException(nameof(httpRouteBase));
            this.RoutingHandler = httpRoutingHandler ?? throw new ArgumentNullException(nameof(httpRoutingHandler));
        }
    }
}
