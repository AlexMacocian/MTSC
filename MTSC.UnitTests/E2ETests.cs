using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Common.Http.ServerModules;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.Server;
using MTSC.Server.Handlers;
using MTSC.Server.UsageMonitors;
using System;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace MTSC.UnitTests
{
    [TestClass]
    public class E2ETests
    {
        public TestContext TestContext { get; set; }
        static Server.Server Server { get; set; }

        [ClassInitialize]
        public static void InitializeServer(TestContext testContext)
        {
            Server = new Server.Server(800)
                .AddHandler(new HttpHandler()
                    .AddHttpModule(new HelloWorldModule())
                    )
                .AddLogger(new ConsoleLogger())
                .AddLogger(new DebugConsoleLogger())
                .AddExceptionHandler(new ExceptionConsoleLogger())
                .AddServerUsageMonitor(new TickrateEnforcer().SetTicksPerSecond(60));
            Server.RunAsync().Start();
        }

        [TestMethod]
        public void TestMethod1()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("http://localhost:800");
            var result = httpClient.GetAsync("").Result;
            Assert.AreEqual(result.StatusCode, System.Net.HttpStatusCode.OK);
        }

        [ClassCleanup]
        public static void CleanupServer()
        {
            Server.Stop();
        }
    }
}
