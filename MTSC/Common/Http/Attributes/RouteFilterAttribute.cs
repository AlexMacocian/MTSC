using System;
using System.Threading.Tasks;

namespace MTSC.Common.Http.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public abstract class RouteFilterAttribute : Attribute
    {
        public virtual RouteEnablerResponse HandleRequest(RouteContext routeContext) => RouteEnablerResponse.Accept;

        public virtual Task<RouteEnablerAsyncResponse> HandleRequestAsync(RouteContext routeContext) => Task.FromResult<RouteEnablerAsyncResponse>(RouteEnablerAsyncResponse.Accept);

        public virtual void HandleResponse(RouteContext routeContext)
        {
        }

        public virtual Task HandleResponseAsync(RouteContext routeContext) => Task.CompletedTask;

        public virtual RouteFilterExceptionHandlingResponse HandleException(RouteContext routeFilterContext, Exception exception)
        {
            return RouteFilterExceptionHandlingResponse.NotHandled;
        }
    }
}
