using MTSC.Common.Http.Attributes;
using MTSC.Common.Http.RoutingModules;
using System.Threading.Tasks;

namespace MTSC.UnitTests.RoutingModules
{
    [NonActioningFilter]
    public class SomeRoutingModule : HttpRouteBase<SomeRoutingRequest, SomeRoutingResponse>
    {
        [FromUrl("someValue")]
        public string SomeValue { get; }
        [FromUrl("intValue")]
        public int IntValue { get; }
        [FromBody]
        public HelloWorldMessage HelloWorldMessage { get; }
        [FromHeaders("Content-Length")]
        public int ContentLength { get; }

        public override Task<SomeRoutingResponse> HandleRequest(SomeRoutingRequest request)
        {
            return Task.FromResult(new SomeRoutingResponse());
        }
    }
}
