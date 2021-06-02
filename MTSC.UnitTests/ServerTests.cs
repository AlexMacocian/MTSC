using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.ServerSide;
using System.Threading;

namespace MTSC.UnitTests
{
    [TestClass]
    [TestCategory("ServerTests")]
    public class ServerTests
    {
        [TestMethod]
        public void Stop()
        {
            Server server = new Server();
            server.RunAsync();
            Thread.Sleep(1000);
            Assert.IsTrue(server.Running);
            server.Stop();
            Thread.Sleep(1000);
            Assert.IsFalse(server.Running);
        }
    }
}
