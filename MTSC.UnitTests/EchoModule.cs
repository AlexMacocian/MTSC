using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.ServerSide;
using System.Threading.Tasks;

namespace MTSC.UnitTests
{
    public class EchoModule : HttpRouteBase
    {
        public override Task<HttpResponse> HandleRequest(HttpRequest request)
        {
            return Task.FromResult(new HttpResponse { BodyString = request.BodyString, StatusCode = HttpMessage.StatusCodes.OK });
        }
    }
}
