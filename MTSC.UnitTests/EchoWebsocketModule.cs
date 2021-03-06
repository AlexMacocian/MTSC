﻿using MTSC.Common.WebSockets.RoutingModules;

namespace MTSC.UnitTests
{
    public class EchoWebsocketModule : WebsocketRouteBase<string, string>
    {
        public override void ConnectionClosed()
        {
        }

        public override void ConnectionInitialized()
        {
        }

        public override void HandleReceivedMessage(string message)
        {
            this.SendMessage(message);
        }

        public override void Tick()
        {
        }
    }
}
