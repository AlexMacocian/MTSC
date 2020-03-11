using MTSC.Common.WebSockets.RoutingModules;
using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;

namespace MTSC.UnitTests
{
    public class EchoWebsocketModule : WebsocketRouteBase<string, string>
    {
        public override void ConnectionClosed(Server server, WebsocketRoutingHandler handler, ClientData client)
        {
            
        }

        public override void ConnectionInitialized(Server server, WebsocketRoutingHandler handler, ClientData client)
        {
            
        }

        public override void HandleReceivedMessage(Server server, WebsocketRoutingHandler handler, ClientData client, string message)
        {
            this.SendMessage(message, client, handler);
        }

        public override void Tick(Server server, WebsocketRoutingHandler handler)
        {
            
        }
    }
}
