using MTSC.Client;
using MTSC.Client.Handlers;
using MTSC.ClientSide;
using MTSC.Common.WebSockets.ClientModules;
using System;

namespace MTSC_TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new Client();
            var websocketHandler = new WebsocketHandler();
            var chatModule = new ChatModule();
            client
                .SetServerAddress("127.0.0.1")
                .SetPort(800)
                .WithReconnectPolicy(ReconnectPolicy.Forever)
                .AddHandler(websocketHandler.AddModule(chatModule))
                //.AddHandler(new EncryptionHandler())
                //.AddHandler(new BroadcastHandler())
                .Connect();
            while (true)
            {
                var message = Console.ReadLine();
                chatModule.SendMessage(websocketHandler, message);
            }
        }
    }
}
