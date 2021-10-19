namespace MTSC.Common.Http
{
    public abstract class RouteEnablerResponse 
    {
        public static RouteEnablerResponseAccept Accept { get; } = new RouteEnablerResponseAccept();
        public static RouteEnablerResponseIgnore Ignore { get; } = new RouteEnablerResponseIgnore();
        public static RouteEnablerResponseError Error(HttpResponse responseMessage)
        {
            return new RouteEnablerResponseError(responseMessage);
        }

        public sealed class RouteEnablerResponseAccept : RouteEnablerResponse
        {
            internal RouteEnablerResponseAccept()
            {
            }
        }

        public sealed class RouteEnablerResponseIgnore : RouteEnablerResponse
        {
            internal RouteEnablerResponseIgnore()
            {
            }
        }

        public sealed class RouteEnablerResponseError : RouteEnablerResponse
        {
            public HttpResponse Response { get; }
            internal RouteEnablerResponseError(HttpResponse responseMessage)
            {
                this.Response = responseMessage;
            }
        }
    }
}
