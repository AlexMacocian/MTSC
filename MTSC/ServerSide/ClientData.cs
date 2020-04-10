using MTSC.Common;
using MTSC.ServerSide.Handlers;
using System;
using System.Net.Security;
using System.Net.Sockets;

namespace MTSC.ServerSide
{
    /// <summary>
    /// Structure containing client information.
    /// </summary>
    public class ClientData : IDisposable, IActiveClient, IQueueHolder<Message>
    {
        private ProducerConsumerQueue<Message> messageQueue = new ProducerConsumerQueue<Message>();

        public TcpClient TcpClient { get; }
        /// <summary>
        /// Latest datetime when a message has been received from the client
        /// </summary>
        public DateTime LastReceivedMessageTime { get; private set; } = DateTime.Now;
        /// <summary>
        /// Latest datetime when a message has been received or sent to the client
        /// </summary>
        public DateTime LastActivityTime { get; private set; } = DateTime.Now;
        /// <summary>
        /// Sets the affinity of the client to a specific handler, ignoring all other handlers
        /// </summary>
        public IHandler Affinity { get; private set; }

        public bool ToBeRemoved { get; set; } = false;
        public SslStream SslStream { get; set; } = null;
        public SafeNetworkStream SafeNetworkStream { get; }
        public ResourceDictionary Resources { get; set; } = new ResourceDictionary();

        IConsumerQueue<Message> IQueueHolder<Message>.ConsumerQueue => messageQueue;

        public ClientData(TcpClient client)
        {
            this.TcpClient = client;
            this.SafeNetworkStream = new SafeNetworkStream(this.TcpClient);
        }
        /// <summary>
        /// Sets the affinity of the client.
        /// </summary>
        /// <param name="handler">Handler to bind to.</param>
        public void SetAffinity(IHandler handler)
        {
            this.Affinity = handler;
        }
        /// <summary>
        /// Resets the affinity of the client.
        /// </summary>
        public void ResetAffinity()
        {
            this.Affinity = null;
        }
        /// <summary>
        /// Resets the affinity if the handler is the one binded.
        /// </summary>
        /// <param name="handler">The handler requesting reset.</param>
        public void ResetAffinityIfMe(IHandler handler)
        {
            if(this.Affinity == handler)
            {
                this.Affinity = null;
            }
        }

        #region IActiveClient Implementation
        void IActiveClient.UpdateLastReceivedMessage()
        {
            LastActivityTime = LastReceivedMessageTime = DateTime.Now;
        }

        void IActiveClient.UpdateLastActivity()
        {
            LastActivityTime = DateTime.Now;
        }
        #endregion
        #region IQueueHolder Imeplementation
        void IQueueHolder<Message>.Enqueue(Message value)
        {
            (messageQueue as IProducerQueue<Message>).Enqueue(value);
        }

        Message IQueueHolder<Message>.Dequeue()
        {
            return (messageQueue as IConsumerQueue<Message>).Dequeue();
        }

        bool IQueueHolder<Message>.TryDequeue(out Message Value)
        {
            return (messageQueue as IConsumerQueue<Message>).TryDequeue(out Value);
        }
        #endregion
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
