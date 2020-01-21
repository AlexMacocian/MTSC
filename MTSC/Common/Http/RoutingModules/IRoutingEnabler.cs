using MTSC.Server;

namespace MTSC.Common.Http.RoutingModules
{
    public interface IRoutingEnabler
    {
        bool RouteEnabled(HttpRequest request, ClientData client);
    }
}
