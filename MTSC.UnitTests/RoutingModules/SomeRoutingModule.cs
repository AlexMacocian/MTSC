using MTSC.Common.Http.RoutingModules;
using System.Threading.Tasks;

namespace MTSC.UnitTests.RoutingModules
{
    public class SomeRoutingModule : HttpRouteBase<SomeRoutingRequest, SomeRoutingResponse>
    {
        public override Task<SomeRoutingResponse> HandleRequest(SomeRoutingRequest request)
        {
            return Task.FromResult(new SomeRoutingResponse());
        }
    }
}
