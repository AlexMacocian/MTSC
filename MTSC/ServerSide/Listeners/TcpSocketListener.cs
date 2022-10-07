using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MTSC.ServerSide.Listeners
{
    public sealed class TcpSocketListener : IListener
    {
        private readonly ConcurrentQueue<Socket> acceptedSockets = new();

        private volatile Socket socket;

        public bool Active => this.socket is not null;
        public EndPoint LocalEndpoint { get => this.socket?.LocalEndPoint; }

        public void Initialize(int port, IPAddress ipAddress)
        {
            lock (this)
            {
                this.socket?.Close();
                this.socket?.Dispose();
                this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                this.socket.Bind(new IPEndPoint(ipAddress, port));
            }
        }

        public Socket AcceptSocket()
        {
            if (this.acceptedSockets.TryDequeue(out var socket))
            {
                return socket;
            }

            throw new InvalidOperationException("Failed to accept client. No client available in queue");
        }

        public bool Pending()
        {
            return this.acceptedSockets.Count > 0;
        }

        public void Start()
        {
            lock (this)
            {
                while(this.socket is not Socket)
                {
                }

                this.socket.Listen(50);
                Task.Run(async () =>
                {
                    while (this.socket is not null)
                    {
                        var clientSocket = await this.socket.AcceptAsync();
                        this.acceptedSockets.Enqueue(clientSocket);
                    }
                });
            }
        }

        public void Stop()
        {
            lock (this)
            {
                this.socket?.Close();
                this.socket?.Dispose();
                this.socket = null;
            }
        }
    }
}
