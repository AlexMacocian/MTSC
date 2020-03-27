﻿using MTSC.Common.Http.ServerModules;
using MTSC.Common.WebSockets.ServerModules;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using MTSC.ServerSide.UsageMonitors;
using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MTSC_TestServer
{    
    class Program
    {
        static void Main(string[] args)
        {
            //X509Certificate2 certificate = new X509Certificate2("localhost.pfx", "psdsd");
            Server server = new Server(443);
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
            //EncryptionHandler encryptionHandler = new EncryptionHandler(rsa);
            server
                //.AddHandler(encryptionHandler)
                .AddLogger(new ConsoleLogger())
                .AddLogger(new DebugConsoleLogger())
                .AddServerUsageMonitor(new TickrateEnforcer().SetTicksPerSecond(60))
                .AddExceptionHandler(new ExceptionConsoleLogger())
                //.AddHandler(new BroadcastHandler())
                .AddHandler(new WebsocketHandler())
                .AddHandler(new HttpHandler().AddHttpModule(new FileServerModule())
                                            .AddHttpModule(new PostModule()))
                .Run();
        }

    }
}
