using MTSC.Common.Http.RoutingModules;
using System;

namespace MTSC.Common.Http.Attributes
{
    public abstract class RouteDataBindingBaseAttribute : Attribute
    {
        /// <summary>
        /// Perform the data binding logic and return the bindable value.
        /// This value will be attached to the decorated property.
        /// The value can be 
        /// </summary>
        /// <param name="module"><see cref="HttpRouteBase"/> object.</param>
        /// <param name="routeContext"><see cref="RouteContext"/> for the current request.</param>
        /// <param name="propertyType"><see cref="Type"/> of property to be binded.</param>
        /// <returns>Object to be binded to the decorated property.</returns>
        public abstract object DataBind(HttpRouteBase route, RouteContext routeContext, Type propertyType);
    }
}
