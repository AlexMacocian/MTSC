using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.Server;
using MTSC.Server.Handlers;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MTSC_TestServer
{    
    class Program
    {
        static void Main(string[] args)
        {
            X509Certificate2 certificate = new X509Certificate2("localhost.pfx", "psdsd");
            Server server = new Server(certificate, 555);
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
            EncryptionHandler encryptionHandler = new EncryptionHandler(rsa, server);
            BroadcastHandler broadcastHandler = new BroadcastHandler(server);
            server
                .AddHandler(encryptionHandler)
                .AddLogger(new ConsoleLogger())
                .AddLogger(new DebugConsoleLogger())
                .AddExceptionHandler(new ExceptionConsoleLogger())
                .AddHandler(broadcastHandler)
                .Run();
        }
    }
}
