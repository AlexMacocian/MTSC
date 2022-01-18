using System;

namespace MTSC.Common.Http.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public abstract class RouteFilterAttribute : Attribute
    {
        public virtual RouteEnablerResponse HandleRequest(RouteContext routeFilterContext) => RouteEnablerResponse.Accept;

        public virtual void HandleResponse(RouteContext routeFilterContext)
        {
        }

        public virtual RouteFilterExceptionHandlingResponse HandleException(RouteContext routeFilterContext, Exception exception)
        {
            return RouteFilterExceptionHandlingResponse.NotHandled;
        }
    }
}
