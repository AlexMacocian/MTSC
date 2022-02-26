using MTSC.Common.Http;
using MTSC.Common.Http.Attributes;
using MTSC.Common.Http.RoutingModules;
using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public sealed class DataBindingException : Exception
    {
        public HttpRouteBase Route { get; }
        public RouteContext RouteContext { get; }
        public RouteDataBindingBaseAttribute RouteDataBindingBaseAttribute { get; }
        public PropertyInfo PropertyInfo { get; }

        public DataBindingException(
            Exception innerException,
            HttpRouteBase route,
            RouteContext routeContext,
            RouteDataBindingBaseAttribute routeDataBindingBaseAttribute,
            PropertyInfo propertyInfo)
            : base("Exception occurred during data binding. Check inner exception for details", innerException)
        {
            this.Route = route;
            this.RouteContext = routeContext;
            this.RouteDataBindingBaseAttribute = routeDataBindingBaseAttribute;
            this.PropertyInfo = propertyInfo;
        }
    }
}
