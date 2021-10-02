using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using Slim;

namespace MTSC.Common.WebSockets.RoutingModules
{
    internal interface ISetWebsocketContext
    {
        void SetServer(Server server);
        void SetHandler(WebsocketRoutingHandler websocketRoutingHandler);
        void SetClient(ClientData clientData);
        void SetScopedServiceProvider(IServiceProvider serviceProvider);
    }
}
