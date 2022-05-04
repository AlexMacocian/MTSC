namespace MTSC.Common.Http
{
    public abstract class RouteEnablerAsyncResponse
    {
        public static RouteEnablerAsyncResponseAccept Accept { get; } = new RouteEnablerAsyncResponseAccept();
        public static RouteEnablerAsyncResponseError Error(HttpResponse responseMessage)
        {
            return new RouteEnablerAsyncResponseError(responseMessage);
        }

        public sealed class RouteEnablerAsyncResponseAccept : RouteEnablerAsyncResponse
        {
            internal RouteEnablerAsyncResponseAccept()
            {
            }
        }

        public sealed class RouteEnablerAsyncResponseError : RouteEnablerAsyncResponse
        {
            public HttpResponse Response { get; }
            internal RouteEnablerAsyncResponseError(HttpResponse responseMessage)
            {
                this.Response = responseMessage;
            }
        }
    }
}
