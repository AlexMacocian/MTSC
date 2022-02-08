using System;
using MTSC.Common.Http;

namespace MTSC.Exceptions
{
    public sealed class FromRouteContextResourcesDataBindingException : Exception
    {
        public Type PropertyType { get; }
        public string Key { get; }
        public RouteContext RouteContext { get; }

        public FromRouteContextResourcesDataBindingException(
            Exception innerException,
            Type propertyType,
            string key,
            RouteContext routeContext)
            : base("Encountered exception when binding data from route context resources", innerException)
        {
            this.PropertyType = propertyType;
            this.Key = key;
            this.RouteContext = routeContext;
        }
    }
}
