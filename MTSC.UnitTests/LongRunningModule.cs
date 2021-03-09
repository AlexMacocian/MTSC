using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.ServerSide;
using System.Threading.Tasks;

namespace MTSC.UnitTests
{
    public class LongRunningModule : HttpRouteBase
    {
        public async override Task<HttpResponse> HandleRequest(HttpRequest request)
        {
            await Task.Delay(5000);
            return new HttpResponse { StatusCode = HttpMessage.StatusCodes.OK };
        }
    }
}
