﻿using MTSC.Client;
using MTSC.Client.Handlers;
using MTSC.Logging;
using System;

namespace MTSC_TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client();
            BroadcastHandler broadcastHandler = new BroadcastHandler(client);
            client
                .SetServerAddress("127.0.0.1")
                .SetPort(555)
                .AddHandler(new EncryptionHandler(client))
                .AddHandler(broadcastHandler)
                .AddLogger(new ConsoleLogger())
                .Connect();
            while (true)
            {
                string line = Console.ReadLine();
                broadcastHandler.Broadcast(line);
            }
        }
    }
}