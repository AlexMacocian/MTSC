using MTSC.Server;

namespace MTSC.Common.Http.RoutingModules
{
    public interface IHttpRoute
    {
        HttpResponse HandleRequest(HttpRequest request, ClientData client);
    }
}
