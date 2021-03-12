using MTSC.Common.WebSockets.RoutingModules;

namespace MTSC.UnitTests
{
    public class EchoWebsocketModule2 : WebsocketRouteBase<byte[], byte[]>
    {
        public override void ConnectionClosed()
        {
        }

        public override void ConnectionInitialized()
        {
        }

        public override void HandleReceivedMessage(byte[] message)
        {
            this.SendMessage(message);
        }

        public override void Tick()
        {
        }
    }
}
