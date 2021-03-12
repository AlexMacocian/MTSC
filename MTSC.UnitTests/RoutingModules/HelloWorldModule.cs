using MTSC.Common.WebSockets.RoutingModules;

namespace MTSC.UnitTests.RoutingModules
{
    public class HelloWorldModule : WebsocketRouteBase<HelloWorldMessage, HelloWorldMessage>
    {
        public override void ConnectionClosed()
        {
        }

        public override void ConnectionInitialized()
        {
        }

        public override void HandleReceivedMessage(HelloWorldMessage message)
        {
            this.SendMessage(message);
        }

        public override void Tick()
        {
        }
    }
}
