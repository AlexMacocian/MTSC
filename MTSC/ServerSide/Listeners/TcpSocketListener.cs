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

        private Socket socket;

        public bool Active => this.socket is not null;
        public EndPoint LocalEndpoint { get => this.socket?.LocalEndPoint; }

        public void Initialize(int port, IPAddress ipAddress)
        {
            this.socket?.Close();
            this.socket?.Dispose();
            this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            this.socket.Bind(new IPEndPoint(ipAddress, port));
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
            this.socket.Listen(50);
            var socket = this.socket;
            Task.Run(async() =>
            {
                while(socket is not null)
                {
                    var clientSocket = await this.socket.AcceptAsync();
                    this.acceptedSockets.Enqueue(clientSocket);
                }
            });
            
        }

        public void Stop()
        {
            this.socket?.Close();
            this.socket?.Dispose();
            this.socket = null;
        }
    }
}
