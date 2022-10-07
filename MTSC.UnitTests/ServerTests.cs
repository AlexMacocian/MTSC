using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.ServerSide;
using MTSC.UnitTests.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

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

        [TestMethod]
        public async Task Stop_Start_RunsInitializationOnlyOnce()
        {
            var serviceOnInitialization = new ServiceOnInitialization();

            var server = new Server(256)
                .AddHandler(serviceOnInitialization);
            server.RunAsync();
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while(cancellationToken.IsCancellationRequested is false)
            {
                if (server.Running)
                {
                    break;
                }
            }

            server.Stop();
            server.RunAsync();
            while (cancellationToken.IsCancellationRequested is false)
            {
                if (server.Running)
                {
                    break;
                }
            }

            serviceOnInitialization.RanOnInitialization.Should().BeTrue();
        }

        [TestMethod]
        public async Task Stop_Start_RunsOnStartupEveryTime()
        {
            var serviceOnStartup = new ServiceOnStartup();

            var server = new Server(256)
                .AddHandler(serviceOnStartup);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await server.RunAsync(cancellationToken.Token);

            cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await server.RunAsync(cancellationToken.Token);

            serviceOnStartup.RanOnStartup.Should().Be(2);
        }
    }
}
