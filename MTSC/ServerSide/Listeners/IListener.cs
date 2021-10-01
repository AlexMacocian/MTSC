using System.Net;
using System.Net.Sockets;

namespace MTSC.ServerSide.Listeners
{
    /// <summary>
    /// Interface for listeners.
    /// </summary>
    public interface IListener
    {
        bool Active { get; }
        EndPoint LocalEndpoint { get; }

        void Initialize(int port, IPAddress iPAddress);
        Socket AcceptSocket();
        bool Pending();
        void Start();
        void Stop();
    }
}
