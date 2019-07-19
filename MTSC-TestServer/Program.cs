using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.Server;
using MTSC.Server.Handlers;
using System;
using System.Security.Cryptography;

namespace MTSC_TestServer
{    
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(555);
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
            EncryptionHandler encryptionHandler = new EncryptionHandler(rsa, server);
            BroadcastHandler broadcastHandler = new BroadcastHandler(server);
            server
                .AddHandler(encryptionHandler)
                .AddLogger(new ConsoleLogger())
                .AddExceptionHandler(new ExceptionConsoleLogger())
                .AddHandler(broadcastHandler)
                .Run();
        }
    }
}
