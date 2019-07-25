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
            
        }

        void IWebsocketModule.ConnectionInitialized(Server.Server server, IHandler handler, ClientData client)
        {
            
        }

        bool IWebsocketModule.HandleReceivedMessage(Server.Server server, IHandler handler, ClientData client, WebsocketMessage receivedMessage)
        {
            receivedMessage.Masked = false;
            //receivedMessage.Opcode = WebsocketMessage.Opcodes.Text;
            foreach(ClientData otherClient in server.Clients)
            {
                (handler as WebsocketHandler).QueueMessage(otherClient, receivedMessage);
            }
            return false;
        }
    }
}
