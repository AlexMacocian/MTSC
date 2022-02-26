using MTSC.Common.Http.RoutingModules;

namespace MTSC.Common.Http.Attributes
{
    public sealed class FromHeadersAttribute : StringRouteDataBindingBaseAttribute
    {
        public string HeaderName { get; }

        public FromHeadersAttribute(string headerName)
        {
            this.HeaderName = headerName;
        }

        public override string GetStringValue(HttpRouteBase route, RouteContext routeContext)
        {
            if (routeContext.HttpRequest.Headers.ContainsHeader(this.HeaderName))
            {
                return routeContext.HttpRequest.Headers[this.HeaderName];
            }

            return null;
        }
    }
}
