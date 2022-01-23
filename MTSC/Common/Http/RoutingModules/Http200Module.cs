using MTSC.ServerSide;
using System.Threading.Tasks;

namespace MTSC.Common.Http.RoutingModules
{
    public sealed class Http200Module : HttpRouteBase
    {
        private readonly Server server;

        public Http200Module(Server server)
        {
            this.server = server;
        }
        
        public override Task<HttpResponse> HandleRequest(HttpRequestContext request)
        {
            return Task.FromResult(this.Ok200);
        }
    }
}
