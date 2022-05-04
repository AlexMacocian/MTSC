using MTSC.Common.Http;
using MTSC.Common.Http.Attributes;
using System.Threading.Tasks;

namespace MTSC.UnitTests.RoutingModules
{
    public sealed class NonActioningFilterAttribute : RouteFilterAttribute
    {
        public static bool RequestCalled { get; private set; }
        public static bool ResponseCalled { get; private set; }
        public static bool RequestAsyncCalled { get; private set; }
        public static bool ResponseAsyncCalled { get; private set; }

        public override RouteEnablerResponse HandleRequest(RouteContext routeContext)
        {
            RequestCalled = true;
            return base.HandleRequest(routeContext);
        }

        public override void HandleResponse(RouteContext routeContext)
        {
            ResponseCalled = true;
            base.HandleResponse(routeContext);
        }

        public override Task<RouteEnablerAsyncResponse> HandleRequestAsync(RouteContext routeContext)
        {
            RequestAsyncCalled = true;
            return base.HandleRequestAsync(routeContext);
        }

        public override Task HandleResponseAsync(RouteContext routeContext)
        {
            ResponseAsyncCalled = true;
            return base.HandleResponseAsync(routeContext);
        }
    }
}
