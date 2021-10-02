using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using Slim;

namespace MTSC.Common.Http.RoutingModules
{
    internal interface ISetHttpContext
    {
        void SetClientData(ClientData clientData);
        void SetServer(Server server);
        void SetHttpRoutingHandler(HttpRoutingHandler httpRoutingHandler);
        void SetScopedServiceProvider(IServiceProvider serviceProvider);
    }
}
