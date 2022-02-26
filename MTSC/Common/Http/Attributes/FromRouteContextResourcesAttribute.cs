using MTSC.Common.Http.RoutingModules;
using System;

namespace MTSC.Common.Http.Attributes
{
    public sealed class FromRouteContextResourcesAttribute : RouteDataBindingBaseAttribute
    {
        public string ResourceKey { get; }

        public FromRouteContextResourcesAttribute(string resourceKey)
        {
            this.ResourceKey = resourceKey;
        }

        public override object DataBind(HttpRouteBase route, RouteContext routeContext, Type propertyType)
        {
            if (routeContext.Resources.TryGetValue(this.ResourceKey, out var resource))
            {
                return resource;
            }

            return null;
        }
    }
}
