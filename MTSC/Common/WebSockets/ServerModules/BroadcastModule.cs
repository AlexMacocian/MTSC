using System;
using System.Collections.Generic;
using System.Text;
using MTSC.Server;
using MTSC.Server.Handlers;

namespace MTSC.Common.WebSockets.ServerModules
{
    public class BroadcastModule : IWebsocketModule
    {
        void IWebsocketModule.ConnectionClosed(Server.Server server, IHandler handler, ClientData client)
        {
            throw new NotImplementedException();
        }

        void IWebsocketModule.ConnectionInitialized(Server.Server server, IHandler handler, ClientData client)
        {
            throw new NotImplementedException();
        }

        bool IWebsocketModule.HandleReceivedMessage(Server.Server server, IHandler handler, ClientData client, WebsocketMessage receivedMessage)
        {
            receivedMessage.Masked = false;
            foreach(ClientData otherClient in server.Clients)
            {
                (handler as WebsocketHandler).QueueMessage(otherClient, receivedMessage.Data);
            }
            return false;
        }
    }
}
