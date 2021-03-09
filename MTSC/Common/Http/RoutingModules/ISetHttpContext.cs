using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.Common.Http.RoutingModules
{
    internal interface ISetHttpContext
    {
        void SetClientData(ClientData clientData);
        void SetServer(Server server);
        void SetHttpRoutingHandler(HttpRoutingHandler httpRoutingHandler);
    }
}
