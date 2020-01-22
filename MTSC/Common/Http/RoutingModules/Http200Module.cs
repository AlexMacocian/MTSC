using MTSC.Server;

namespace MTSC.Common.Http.RoutingModules
{
    public class Http200Module : IHttpRoute
    {
        HttpResponse IHttpRoute.HandleRequest(HttpRequest request, ClientData client)
        {
            return new HttpResponse { StatusCode = HttpMessage.StatusCodes.OK };
        }
    }
}
