using FluentAssertions;
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
            var server = new Server(256);
            server.RunAsync();
            Thread.Sleep(1000);
            Assert.IsTrue(server.Running);
            server.Stop();
            Thread.Sleep(1000);
            Assert.IsFalse(server.Running);
        }

        [TestMethod]
        public void CancellationToken_StopsServer()
        {
            var server = new Server(256);
            var cts = new CancellationTokenSource();
            var runningTask = server.RunAsync(cts.Token);
            Thread.Sleep(1000);
            server.Running.Should().BeTrue();

            cts.Cancel();
            runningTask.Wait(1000);
            runningTask.IsCompleted.Should().BeTrue();
        }
    }
}
