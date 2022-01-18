using System;

namespace MTSC.Common.Http
{
    public abstract class RouteFilterExceptionHandlingResponse
    {
        internal RouteFilterExceptionHandlingResponse()
        {
        }

        public sealed class NotHandledResponse : RouteFilterExceptionHandlingResponse
        {
            internal NotHandledResponse()
            {
            }
        }
        public sealed class HandledResponse : RouteFilterExceptionHandlingResponse
        {
            public HttpResponse HttpResponse { get; }

            internal HandledResponse(HttpResponse httpResponse)
            {
                this.HttpResponse = httpResponse ?? throw new ArgumentNullException(nameof(httpResponse));
            }
        }

        public static NotHandledResponse NotHandled => new();
        public static HandledResponse Handled(HttpResponse httpResponse) => new(httpResponse);
    }
}
