using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using System.Threading.Tasks;

namespace MTSC.UnitTests.RoutingModules
{
    public class ExceptionThrowingModule : HttpRouteBase
    {
        public override Task<HttpResponse> HandleRequest(HttpRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}
