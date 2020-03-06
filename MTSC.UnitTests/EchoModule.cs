using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.ServerSide;

namespace MTSC.UnitTests
{
    public class EchoModule : HttpRouteBase
    {
        public override HttpResponse HandleRequest(HttpRequest request, ClientData client, ServerSide.Server server)
        {
            return new HttpResponse { BodyString = request.BodyString, StatusCode = HttpMessage.StatusCodes.OK };
        }
    }
}
