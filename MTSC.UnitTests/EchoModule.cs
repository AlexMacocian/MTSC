using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.Server;

namespace MTSC.UnitTests
{
    public class EchoModule : HttpRouteBase
    {
        public override HttpResponse HandleRequest(HttpRequest request, ClientData client, Server.Server server)
        {
            return new HttpResponse { BodyString = request.BodyString, StatusCode = HttpMessage.StatusCodes.OK };
        }
    }
}
