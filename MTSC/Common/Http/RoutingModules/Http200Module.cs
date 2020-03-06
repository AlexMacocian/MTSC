using MTSC.ServerSide;

namespace MTSC.Common.Http.RoutingModules
{
    public sealed class Http200Module : HttpRouteBase
    {
        public override HttpResponse HandleRequest(HttpRequest request, ClientData client, ServerSide.Server server)
        {
            return new HttpResponse { StatusCode = HttpMessage.StatusCodes.OK };
        }
    }
}
