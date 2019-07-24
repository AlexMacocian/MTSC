using MTSC.Client;
using MTSC.Client.Handlers;
using MTSC.Common.WebSockets.ClientModules;
using MTSC.Logging;
using System;

namespace MTSC_TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client();
            WebsocketHandler websocketHandler = new WebsocketHandler();
            ChatModule chatModule = new ChatModule();
            client
                .SetServerAddress("127.0.0.1")
                .SetPort(80)
                .AddHandler(websocketHandler.AddModule(chatModule))
                //.AddHandler(new EncryptionHandler())
                //.AddHandler(new BroadcastHandler())
                .AddLogger(new ConsoleLogger())
                .AddLogger(new DebugConsoleLogger())
                .Connect();
            while (true)
            {
                string message = Console.ReadLine();
                chatModule.SendMessage(websocketHandler, message);
            }
        }
    }
}
