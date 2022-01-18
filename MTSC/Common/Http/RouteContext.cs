using MTSC.ServerSide;
using System.Threading;

namespace MTSC.Common.Http
{
    public sealed class RouteContext
    {
        public Server Server { get; }
        public HttpRequest HttpRequest { get; }
        public ClientData Client { get; }
        public HttpResponse HttpResponse { get; set; }
        public CancellationToken CancelRequest => this.Client.CancellationToken;

        public RouteContext(
            Server server,
            HttpRequest httpRequest,
            ClientData clientData)
        {
            this.Server = server;
            this.HttpRequest = httpRequest;
            this.Client = clientData;
        }
    }
}
