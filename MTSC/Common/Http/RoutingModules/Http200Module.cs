using MTSC.ServerSide;
using System.Threading.Tasks;

namespace MTSC.Common.Http.RoutingModules
{
    public sealed class Http200Module : HttpRouteBase
    {
        public override Task<HttpResponse> HandleRequest(HttpRequest request, ClientData client, ServerSide.Server server)
        {
            return Task.FromResult(new HttpResponse { StatusCode = HttpMessage.StatusCodes.OK });
        }
    }
}
