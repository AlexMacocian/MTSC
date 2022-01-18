using MTSC.Common.Http;
using MTSC.Common.Http.Attributes;

namespace MTSC.UnitTests.RoutingModules
{
    public sealed class NonActioningFilterAttribute : RouteFilterAttribute
    {
        public static bool RequestCalled { get; private set; }
        public static bool ResponseCalled { get; private set; }

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
    }
}
