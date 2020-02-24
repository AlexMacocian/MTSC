using MTSC.Server;

namespace MTSC.Common.Http.RoutingModules
{
    public interface IHttpRoute<T> where T : ITemplatedHttpRequest
    {
        HttpResponse HandleRequest(T request, ClientData client, Server.Server server);
    }
}
