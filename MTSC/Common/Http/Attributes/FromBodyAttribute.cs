using MTSC.Common.Http.RoutingModules;

namespace MTSC.Common.Http.Attributes
{
    public sealed class FromBodyAttribute : StringRouteDataBindingBaseAttribute
    {
        public override string GetStringValue(HttpRouteBase route, RouteContext routeContext)
        {
            return routeContext.HttpRequest.BodyString;
        }
    }
}
