using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MTSC.ServerSide.Listeners;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MTSC.UnitTests.Listener
{
    [TestClass]
    public class TcpSocketListenerTests
    {
        private const int Port = 256;
        private readonly TcpSocketListener listener = new();

        [TestMethod]
        public void Start_StartsAndAcceptsSockets()
        {
            this.InitializeListener();
            var client = this.ConnectClient();
            var acceptedSocket = this.WaitAndAcceptPendingClient();
            client.Should().NotBeNull();
            acceptedSocket.Should().NotBeNull();
        }
        [TestMethod]
        public void Stop_StopsUnderlyingListener()
        {
            this.InitializeListener();
            var client = this.ConnectClient();
            var acceptedSocket = this.WaitAndAcceptPendingClient();
            acceptedSocket.Should().NotBeNull();

            this.StopListener();
            acceptedSocket.Close();
            client.Close();

            var connectAction = new Action(() =>
            {
                this.ConnectClient();
            });

            connectAction.Should().Throw<SocketException>();
        }
        [TestMethod]
        public void AcceptSocket_ThrowsWhenNoPending()
        {
            this.InitializeListener();

            var acceptClient = new Action(() =>
            {
                this.listener.AcceptSocket();
            });

            acceptClient.Should().Throw<InvalidOperationException>();
        }
        [TestMethod]
        public void Pneding_ReturnsFalseWhenNoClients()
        {
            this.InitializeListener();

            this.listener.Pending().Should().BeFalse();
        }
        [TestMethod]
        public async Task Pending_ReturnsTrueWhenAvailableClients()
        {
            this.InitializeListener();
            this.ConnectClient();

            await Task.Delay(1000);

            this.listener.Pending().Should().BeTrue();
        }
        [TestMethod]
        public void Active_ReturnsFalseWhenNotRunning()
        {
            this.listener.Active.Should().BeFalse();

            this.InitializeListener();
            this.StopListener();

            this.listener.Active.Should().BeFalse();
        }
        [TestMethod]
        public void Active_ReturnsTrueWhenRunning()
        {
            this.InitializeListener();
            this.listener.Active.Should().BeTrue();
        }
        [TestMethod]
        public void LocalEndpoint_ReturnsNullWhenNotRunning()
        {
            this.listener.LocalEndpoint.Should().BeNull();

            this.InitializeListener();
            this.StopListener();

            this.listener.LocalEndpoint.Should().BeNull();
        }
        [TestMethod]
        public void LocalEndpoint_ReturnsEndpointWhenRunning()
        {
            this.InitializeListener();

            this.listener.LocalEndpoint.Should().NotBeNull();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.listener.Stop();
        }

        private TcpClient ConnectClient()
        {
            return new TcpClient("127.0.0.1", Port);
        }
        private void InitializeListener()
        {
            this.listener.Initialize(Port, IPAddress.Loopback);
            this.listener.Start();
        }
        private void StopListener()
        {
            this.listener.Stop();
        }
        private Socket WaitAndAcceptPendingClient()
        {
            while(this.listener.Pending() is false)
            {
            }

            return this.listener.AcceptSocket();
        }
    }
}
