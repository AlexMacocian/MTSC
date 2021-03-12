using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.Common.WebSockets.RoutingModules
{
    internal interface ISetWebsocketContext
    {
        void SetServer(Server server);
        void SetHandler(WebsocketRoutingHandler websocketRoutingHandler);
        void SetClient(ClientData clientData);
    }
}
