using MTSC.Common.Http.RoutingModules;

namespace MTSC.Common.Http.Attributes
{
    public class FromUrlAttribute : StringRouteDataBindingBaseAttribute
    {
        public string Placeholder { get; }

        public FromUrlAttribute(string placeholder)
        {
            this.Placeholder = placeholder;
        }

        public override string GetStringValue(HttpRouteBase route, RouteContext routeContext)
        {
            if (routeContext.UrlValues.TryGetValue(this.Placeholder, out var value))
            {
                return value;
            }

            return null;
        }
    }
}
