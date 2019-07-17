using MTSC.Client.Handlers;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MTSC.Client
{
    /// <summary>
    /// Base class for TCP Client.
    /// </summary>
    public class Client
    {
        #region Fields
        string address;
        int port;
        TcpClient tcpClient;
        CancellationTokenSource cancelMonitorToken;
        List<IHandler> handlers = new List<IHandler>();
        #endregion
        #region Properties
        public bool Connected
        {
            get
            {
                if (tcpClient != null)
                    return tcpClient.Connected;
                else
                    return false;
            }
        }
        public string Address { get => address; }
        public int Port { get => port; }
        #endregion
        #region Constructors
        public Client()
        {

        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Sets the server address.
        /// </summary>
        /// <param name="address">Address of the server.</param>
        /// <returns>This client object.</returns>
        public Client SetServerAddress(string address)
        {
            this.address = address;
            return this;
        }
        /// <summary>
        /// Sets the server port.
        /// </summary>
        /// <param name="port">Port of the server.</param>
        /// <returns>This client object.</returns>
        public Client SetPort(int port)
        {
            this.port = port;
            return this;
        }
        /// <summary>
        /// Adds a handler onto the client.
        /// </summary>
        /// <param name="handler">Connection handler object.</param>
        /// <returns>This client object.</returns>
        public Client AddHandler(IHandler handler)
        {
            handlers.Add(handler);
            return this;
        }
        /// <summary>
        /// Attemps to connect to the specified server.
        /// </summary>
        /// <returns>True if connection was successful.</returns>
        public bool Connect()
        {
            if(tcpClient != null)
            {
                cancelMonitorToken?.Cancel();
                tcpClient.Dispose();
            }
            tcpClient = new TcpClient();
            tcpClient.Connect(address, port);
            foreach(IHandler handler in handlers)
            {
                if (!handler.InitializeConnection(tcpClient))
                {
                    return false;
                }
            }
            cancelMonitorToken = new CancellationTokenSource();
            Task.Run(MonitorConnection, cancelMonitorToken.Token);
            return true;
        }
        /// <summary>
        /// Attempts to connect to the specified server.
        /// </summary>
        /// <returns>True if the connection was successful.</returns>
        public Task<bool> ConnectAsync()
        {
            return new Task<bool>(Connect);
        }
        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            cancelMonitorToken?.Cancel();
            tcpClient.Dispose();
        }
        #endregion
        #region Private Methods
        private void MonitorConnection()
        {
            while (true)
            {
                if(tcpClient.Available > 0)
                {
                    /*
                     * When a message has been received, process it.
                     */
                    Message message = CommunicationPrimitives.GetMessage(tcpClient);
                    /*
                     * Preprocess message.
                     */
                    foreach(IHandler handler in handlers)
                    {
                        if(handler.PreHandleReceivedMessage(tcpClient, out message))
                        {
                            break;
                        }
                    }
                    /*
                     * Process the final message structure.
                     */
                    foreach(IHandler handler in handlers)
                    {
                        if(handler.HandleReceivedMessage(tcpClient, message))
                        {
                            break;
                        }
                    }
                }
            }
        }
        #endregion
    }
}
