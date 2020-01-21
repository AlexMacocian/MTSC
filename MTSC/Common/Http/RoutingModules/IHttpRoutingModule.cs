using MTSC.Server;

namespace MTSC.Common.Http.RoutingModules
{
    public interface IHttpRoutingModule
    {
        HttpResponse HandleRequest(HttpRequest request, ClientData client);
    }
}
