using MTSC.Common.Ftp.FtpModules;
using MTSC.Common.Http.RoutingModules;
using MTSC.Common.Http.ServerModules;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using MTSC.ServerSide.UsageMonitors;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MTSC_TestServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server(800);
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
            server
                //.WithCertificate(new X509Certificate2("powershellcert.pfx", "123"))
                .AddLogger(new ConsoleLogger())
                .AddLogger(new DebugConsoleLogger())
                .AddServerUsageMonitor(new TickrateEnforcer()
                    .SetTicksPerSecond(60)
                    .SetSilent(true))
                .AddExceptionHandler(new ExceptionConsoleLogger())
                .AddHandler(new HttpRoutingHandler()
                    .AddRoute(MTSC.Common.Http.HttpMessage.HttpMethods.Get, "hello", new Http200Module()))
                .AddHandler(new FtpHandler()
                    .AddModule(new AuthenticationModule())
                    .AddModule(new SystModule())
                    .AddModule(new DataConnectionModule())
                    .AddModule(new DirectoryModule()
                        .WithRootPath("src"))
                    .AddModule(new FileModule())
                    .AddModule(new QuitModule())
                    .AddModule(new UnknownCommandModule()))
                .WithClientCertificate(true)
                .Run();
        }

    }
}
