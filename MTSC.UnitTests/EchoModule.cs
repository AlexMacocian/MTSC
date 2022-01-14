using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.ServerSide;
using System.Threading.Tasks;

namespace MTSC.UnitTests
{
    public class EchoModule : HttpRouteBase
    {
        public override Task<HttpResponse> HandleRequest(HttpRequestContext request)
        {
            return Task.FromResult(new HttpResponse { BodyString = request.Request.BodyString, StatusCode = HttpMessage.StatusCodes.OK });
        }
    }
}
