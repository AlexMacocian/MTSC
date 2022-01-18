using MTSC.Common;
using MTSC.ServerSide.Handlers;
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;

namespace MTSC.ServerSide
{
    /// <summary>
    /// Structure containing client information.
    /// </summary>
    public class ClientData : IDisposable, IActiveClient, IQueueHolder<Message>
    {
        private readonly ProducerConsumerQueue<Message> messageQueue = new();

        private CancellationTokenSource cancellationTokenSource = new();

        public CancellationToken CancellationToken => this.cancellationTokenSource.Token;
        public Socket Socket { get; }
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
        /// <summary>
        /// Setting this property to true will mark the client to be removed in the next server cycle
        /// </summary>
        public bool ToBeRemoved { get; set; } = false;
        /// <summary>
        /// <see cref="System.Net.Security.SslStream"/> in case the server uses SSL
        /// </summary>
        public SslStream SslStream { get; set; } = null;
        /// <summary>
        /// Layer over client's <see cref="NetworkStream"/> to protect the underlying stream of <see cref="TimeoutException"/>
        /// </summary>
        public TimeoutSuppressedStream SafeNetworkStream { get; }
        /// <summary>
        /// Dictionary of Resources that the client holds
        /// </summary>
        public ResourceDictionary Resources { get; set; } = new ResourceDictionary();

        IConsumerQueue<Message> IQueueHolder<Message>.ConsumerQueue => this.messageQueue;
        bool IActiveClient.ReadingData { get; set; }

        public ClientData(Socket socket)
        {
            this.Socket = socket;
            var ns = new NetworkStream(this.Socket);
            this.SafeNetworkStream = new TimeoutSuppressedStream(ns);
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
            this.LastActivityTime = this.LastReceivedMessageTime = DateTime.Now;
        }

        void IActiveClient.UpdateLastActivity()
        {
            this.LastActivityTime = DateTime.Now;
        }
        #endregion
        #region IQueueHolder Imeplementation
        void IQueueHolder<Message>.Enqueue(Message value)
        {
            (this.messageQueue as IProducerQueue<Message>).Enqueue(value);
        }

        Message IQueueHolder<Message>.Dequeue()
        {
            return (this.messageQueue as IConsumerQueue<Message>).Dequeue();
        }

        bool IQueueHolder<Message>.TryDequeue(out Message Value)
        {
            return (this.messageQueue as IConsumerQueue<Message>).TryDequeue(out Value);
        }
        #endregion
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.cancellationTokenSource.Cancel();
                    this.cancellationTokenSource = null;
                    this.SafeNetworkStream?.Dispose();
                    this.SslStream?.Dispose();
                    this.Resources?.Dispose();
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }
        #endregion
    }
}
