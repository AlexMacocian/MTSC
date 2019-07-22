using MTSC.Client.Handlers;
using MTSC.Exceptions;
using MTSC.Logging;
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
        List<ILogger> loggers = new List<ILogger>();
        List<IExceptionHandler> exceptionHandlers = new List<IExceptionHandler>();
        Queue<byte[]> messageQueue = new Queue<byte[]>();
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
        /// Add a message to the message queue.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        public void QueueMessage(byte[] message)
        {
            messageQueue.Enqueue(message);
        }
        /// <summary>
        /// Logs the message onto the associated loggers.
        /// </summary>
        /// <param name="log">Message to be logged.</param>
        public void Log(string log)
        {
            foreach (ILogger logger in loggers)
            {
                logger.Log(log);
            }
        }
        /// <summary>
        /// Logs the debug message onto the associated loggers.
        /// </summary>
        /// <param name="debugMessage"></param>
        public void LogDebug(string debugMessage)
        {
            foreach (ILogger logger in loggers)
            {
                logger.LogDebug(debugMessage);
            }
        }
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
        /// Adds an exception handler to the client.
        /// </summary>
        /// <param name="handler">Exception handler to be added.</param>
        /// <returns>This client object.</returns>
        public Client AddExceptionHandler(IExceptionHandler handler)
        {
            exceptionHandlers.Add(handler);
            return this;
        }
        /// <summary>
        /// Adds a logger onto the client.
        /// </summary>
        /// <param name="logger">Logger to be added.</param>
        /// <returns>This client object.</returns>
        public Client AddLogger(ILogger logger)
        {
            loggers.Add(logger);
            return this;
        }
        /// <summary>
        /// Attemps to connect to the specified server.
        /// </summary>
        /// <returns>True if connection was successful.</returns>
        public bool Connect()
        {
            try
            {
                if (tcpClient != null)
                {
                    cancelMonitorToken?.Cancel();
                    tcpClient.Dispose();
                }
                tcpClient = new TcpClient();
                tcpClient.Connect(address, port);
                foreach(ILogger logger in loggers)
                {
                    logger.Log("Connected to: " + tcpClient.Client.RemoteEndPoint.ToString());
                }
                foreach (IHandler handler in handlers)
                {
                    if (!handler.InitializeConnection(tcpClient))
                    {
                        return false;
                    }
                }
                cancelMonitorToken = new CancellationTokenSource();
                Task.Run(new Action(MonitorConnection), cancelMonitorToken.Token);
                return true;
            }
            catch(Exception e)
            {
                foreach(IExceptionHandler exceptionHandler in exceptionHandlers)
                {
                    exceptionHandler.HandleException(e);
                }
                return false;
            }
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
                try
                {
                    if(messageQueue.Count > 0)
                    {
                        byte[] messagebytes = messageQueue.Dequeue();
                        Message sendMessage = CommunicationPrimitives.BuildMessage(messagebytes);
                        for(int i = handlers.Count - 1; i >= 0; i--)
                        {
                            IHandler handler = handlers[i];
                            handler.HandleSendMessage(tcpClient, ref sendMessage);
                        }
                        CommunicationPrimitives.SendMessage(tcpClient, sendMessage);
                    }
                    if (tcpClient.Available > 0)
                    {
                        /*
                         * When a message has been received, process it.
                         */
                        Message message = CommunicationPrimitives.GetMessage(tcpClient);
                        LogDebug("Received a message of size: " + message.MessageLength);
                        /*
                         * Preprocess message.
                         */
                        foreach (IHandler handler in handlers)
                        {
                            if (handler.PreHandleReceivedMessage(tcpClient, ref message))
                            {
                                break;
                            }
                        }
                        /*
                         * Process the final message structure.
                         */
                        foreach (IHandler handler in handlers)
                        {
                            if (handler.HandleReceivedMessage(tcpClient, message))
                            {
                                break;
                            }
                        }
                    }
                    foreach (IHandler handler in handlers)
                    {
                        handler.Tick(tcpClient);
                    }
                }
                catch(Exception e)
                {
                    LogDebug("Exception: " + e.Message);
                    LogDebug("Stacktrace: " + e.StackTrace);
                    foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
                    {
                        exceptionHandler.HandleException(e);
                    }
                }
            }
        }
        #endregion
    }
}
