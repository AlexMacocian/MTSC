using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.ServerSide.Handlers;
using MTSC.ServerSide.UsageMonitors;

namespace MTSC.UnitTests
{
    [TestClass]
    public class ResourcesTests
    {
        public static ServerSide.Server server;
        [ClassInitialize]
        public static void InitializeServer(TestContext testContext)
        {
            server = new ServerSide.Server();
        }
        [TestMethod]
        public void AddAndGetResource()
        {
            StringResource resource = new StringResource { Value = "hello" };
            server.WithResource(resource);
            server.AddHandler(new HttpHandler())
                .AddExceptionHandler(new ExceptionConsoleLogger())
                .AddLogger(new ConsoleLogger())
                .AddServerUsageMonitor(new TickrateEnforcer());
            var gotResource = server.GetResource<StringResource>();
            Assert.AreEqual(gotResource, resource);
            Assert.IsNotNull(server.GetExceptionHandler<ExceptionConsoleLogger>());
            Assert.IsNotNull(server.GetHandler<HttpHandler>());
            Assert.IsNotNull(server.GetLogger<ConsoleLogger>());
            Assert.IsNotNull(server.GetServerUsageMonitor<TickrateEnforcer>());
        }
    }
}
