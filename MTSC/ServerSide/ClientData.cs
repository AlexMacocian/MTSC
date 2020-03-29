using System;
using System.Net.Security;
using System.Net.Sockets;

namespace MTSC.ServerSide
{
    /// <summary>
    /// Structure containing client information.
    /// </summary>
    public class ClientData : IDisposable, IActiveClient
    {
        public TcpClient TcpClient;
        /// <summary>
        /// Latest datetime when a message has been received from the client
        /// </summary>
        public DateTime LastReceivedMessageTime { get; private set; } = DateTime.Now;
        /// <summary>
        /// Latest datetime when a message has been received or sent to the client
        /// </summary>
        public DateTime LastActivityTime { get; private set; } = DateTime.Now;
        public bool ToBeRemoved = false;
        public SslStream SslStream = null;
        public ResourceDictionary Resources = new ResourceDictionary();

        public ClientData(TcpClient client)
        {
            this.TcpClient = client;
        }

        void IActiveClient.UpdateLastReceivedMessage()
        {
            LastActivityTime = LastReceivedMessageTime = DateTime.Now;
        }

        void IActiveClient.UpdateLastActivity()
        {
            LastActivityTime = DateTime.Now;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                TcpClient?.Dispose();
                SslStream?.Dispose();
                Resources?.Dispose();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ClientData()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
