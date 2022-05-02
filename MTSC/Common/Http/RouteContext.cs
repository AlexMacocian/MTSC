using MTSC.ServerSide;
using Slim;
using System.Collections.Generic;
using System.Threading;

namespace MTSC.Common.Http
{
    public sealed class RouteContext
    {
        public Server Server { get; }
        public HttpRequest HttpRequest { get; }
        public ClientData Client { get; }
        public Dictionary<string, string> UrlValues { get; }
        public IServiceProvider ScopedServiceProvider { get; }
        public HttpResponse HttpResponse { get; set; }
        public CancellationToken CancelRequest => this.Client.CancellationToken;
        public Dictionary<string, object> Resources { get; set; } = new();
        

        public RouteContext(
            Server server,
            HttpRequest httpRequest,
            ClientData clientData,
            IServiceProvider scopedServiceProvider,
            Dictionary<string, string> urlValues)
        {
            this.Server = server;
            this.HttpRequest = httpRequest;
            this.Client = clientData;
            this.ScopedServiceProvider = scopedServiceProvider;
            this.UrlValues = urlValues;
        }
    }
}
