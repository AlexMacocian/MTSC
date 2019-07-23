using MTSC.Client;
using MTSC.Client.Handlers;
using MTSC.Logging;
using System;

namespace MTSC_TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client(true);
            client
                .SetServerAddress("127.0.0.1")
                .SetPort(555)
                .AddHandler(new EncryptionHandler())
                //.AddHandler(new BroadcastHandler())
                .AddLogger(new ConsoleLogger())
                .AddLogger(new DebugConsoleLogger())
                .Connect();
        }
    }
}
