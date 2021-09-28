using MTSC.Client.Handlers;
using MTSC.ClientSide;
using MTSC.Common;
using MTSC.Exceptions;
using MTSC.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace MTSC.Client
{
    /// <summary>
    /// Base class for TCP Client.
    /// </summary>
    public sealed class Client
    {
        #region Fields
        private TcpClient tcpClient;
        private CancellationTokenSource cancelMonitorToken;
        private readonly List<IHandler> handlers = new();
        private readonly List<ILogger> loggers = new();
        private readonly List<IExceptionHandler> exceptionHandlers = new();
        private readonly Queue<byte[]> messageQueue = new();
        private SslStream sslStream = null;
        private TimeoutSuppressedStream safeNetworkStream = null;
        #endregion
        #region Properties
        public bool Connected
        {
            get
            {
                if (this.tcpClient != null)
                {
                    return this.tcpClient.Connected;
                }
                else
                {
                    return false;
                }
            }
        }
        public string Address { get; private set; }
        public int Port { get; private set; }
        public bool ForceSsl { get; private set; }
        public Func<IPAddress[], IPAddress> AddressResolution { get; set; } = (addresses) => addresses[0];
        public Func<string, bool> SchemeSslFilter { get; set; } = RequiresSsl;
        /// <summary>
        /// Callback function used to determine if the remote certificate is valid.
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }
        public ReconnectPolicy ReconnectPolicy { get; set; } = ReconnectPolicy.Never;
        #endregion
        #region Constructors
        public Client(bool useSsl = false)
        {
            this.ForceSsl = useSsl;
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Sets the <see cref="ReconnectPolicy"/>.
        /// </summary>
        /// <param name="reconnectPolicy"></param>
        /// <returns>This client.</returns>
        public Client WithReconnectPolicy(ReconnectPolicy reconnectPolicy)
        {
            this.ReconnectPolicy = reconnectPolicy;
            return this;
        }
        /// <summary>
        /// Sets the certificate validation callback for ssl connection.
        /// </summary>
        /// <param name="remoteCertificateValidationCallback"></param>
        /// <returns>This client object.</returns>
        public Client WithRemoteCertificateValidationCallback(RemoteCertificateValidationCallback remoteCertificateValidationCallback)
        {
            this.CertificateValidationCallback = remoteCertificateValidationCallback;
            return this;
        }
        /// <summary>
        /// Sets the <see cref="SchemeSslFilter"/> property.
        /// </summary>
        /// <param name="schemeFilter">Function that should return true if the provided scheme should use Ssl.</param>
        /// <returns>This client object.</returns>
        public Client WithSchemeSslFilter(Func<string, bool> schemeFilter)
        {
            this.SchemeSslFilter = schemeFilter;
            return this;
        }
        /// <summary>
        /// Sets the <see cref="AddressResolution"/> property.
        /// </summary>
        /// <param name="addressResolutionFunc">Function to perform address resolution when the DNS returns multiple addresses.</param>
        /// <returns>This client object.</returns>
        public Client WithAddressResolution(Func<IPAddress[], IPAddress> addressResolutionFunc)
        {
            this.AddressResolution = addressResolutionFunc;
            return this;
        }
        public Client WithSsl(bool ssl)
        {
            this.ForceSsl = ssl;
            return this;
        }
        /// <summary>
        /// Add a message to the message queue.
        /// </summary>
        /// <param name="message">Message to be sent.</param>
        public void QueueMessage(byte[] message)
        {
            this.messageQueue.Enqueue(message);
        }
        /// <summary>
        /// Logs the message onto the associated loggers.
        /// </summary>
        /// <param name="log">Message to be logged.</param>
        public void Log(string log)
        {
            foreach (var logger in this.loggers)
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
            foreach (var logger in this.loggers)
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
            this.Address = address;
            return this;
        }
        /// <summary>
        /// Sets the server port.
        /// </summary>
        /// <param name="port">Port of the server.</param>
        /// <returns>This client object.</returns>
        public Client SetPort(int port)
        {
            this.Port = port;
            return this;
        }
        /// <summary>
        /// Adds a handler onto the client.
        /// </summary>
        /// <param name="handler">Connection handler object.</param>
        /// <returns>This client object.</returns>
        public Client AddHandler(IHandler handler)
        {
            this.handlers.Add(handler);
            return this;
        }
        /// <summary>
        /// Adds an exception handler to the client.
        /// </summary>
        /// <param name="handler">Exception handler to be added.</param>
        /// <returns>This client object.</returns>
        public Client AddExceptionHandler(IExceptionHandler handler)
        {
            this.exceptionHandlers.Add(handler);
            return this;
        }
        /// <summary>
        /// Adds a logger onto the client.
        /// </summary>
        /// <param name="logger">Logger to be added.</param>
        /// <returns>This client object.</returns>
        public Client AddLogger(ILogger logger)
        {
            this.loggers.Add(logger);
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
                if (this.tcpClient != null)
                {
                    this.cancelMonitorToken?.Cancel();
                    this.tcpClient.Dispose();
                }

                this.tcpClient = new TcpClient();
                var shouldUseSsl = this.ForceSsl;
                if (!IPAddress.TryParse(this.Address, out var ipAddress))
                {
                    if (!Uri.TryCreate(this.Address, UriKind.Absolute, out var addressUri))
                    {
                        throw new InvalidOperationException($"{this.Address} is not an IP Address nor a valid URI");
                    }

                    var potentialAddresses = Dns.GetHostAddresses(addressUri.DnsSafeHost);
                    if (potentialAddresses.Length <= 0)
                    {
                        throw new InvalidOperationException($"Cannot connect to {this.Address}. Could not find any ip address related to specified host name.");
                    }

                    ipAddress = this.AddressResolution(potentialAddresses);
                    if (this.SchemeSslFilter(addressUri.Scheme))
                    {
                        shouldUseSsl = true;
                    }

                    this.Address = addressUri.Host;
                }

                var ipEndpoint = new IPEndPoint(ipAddress, this.Port);
                this.tcpClient.Connect(ipEndpoint);
                this.safeNetworkStream = new TimeoutSuppressedStream(this.tcpClient);
                if (shouldUseSsl)
                {
                    this.CertificateValidationCallback = this.CertificateValidationCallback ?? new RemoteCertificateValidationCallback(ValidateServerCertificate);
                    this.sslStream = new SslStream(this.safeNetworkStream, true, this.CertificateValidationCallback, null);
                    this.sslStream.AuthenticateAsClient(this.Address);
                }

                foreach(var logger in this.loggers)
                {
                    logger.Log("Connected to: " + this.tcpClient.Client.RemoteEndPoint.ToString());
                }

                foreach (var handler in this.handlers)
                {
                    if (!handler.InitializeConnection(this))
                    {
                        return false;
                    }
                }

                this.cancelMonitorToken = new CancellationTokenSource();
                Task.Run(new Action(this.MonitorConnection), this.cancelMonitorToken.Token);
                return true;
            }
            catch(Exception e)
            {
                this.LogDebug("Exception: " + e.Message);
                this.LogDebug("Stacktrace: " + e.StackTrace);
                foreach (var exceptionHandler in this.exceptionHandlers)
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
            return Task.Run(this.Connect);
        }
        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            this.cancelMonitorToken?.Cancel();
            this.tcpClient.Dispose();
        }
        #endregion
        #region Private Methods
        /// <summary>
        /// Delegate used to validate server certificate.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns>True if certificate is valid.</returns>
        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            return true;
        }

        private static bool RequiresSsl(string scheme)
        {
            return scheme == "wss" || scheme == "https";
        }

        private void MonitorConnection()
        {
            while (true)
            {
                try
                {
                    this.SendQueuedMessages();
                    this.ReceiveMessages();
                    this.TickHandlers();
                }
                catch (IOException fatalException)
                {
                    this.HandleException(fatalException);
                    break;
                }
                catch(Exception e)
                {
                    this.HandleException(e);
                }

                Thread.Sleep(33);
            }

            if (this.ReconnectPolicy == ReconnectPolicy.Forever)
            {
                do
                {
                    this.Log("Connection failed. Attempting to reconnect...");
                } while (this.Connect() is false);
            }
            else if (this.ReconnectPolicy == ReconnectPolicy.Once)
            {
                this.Log("Connection failed. Attempting to reconnect...");
                this.Connect();
            }
        }

        private void SendQueuedMessages()
        {
            if (this.messageQueue.Count > 0)
            {
                var messagebytes = this.messageQueue.Dequeue();
                var sendMessage = CommunicationPrimitives.BuildMessage(messagebytes);
                for (var i = this.handlers.Count - 1; i >= 0; i--)
                {
                    var handler = this.handlers[i];
                    handler.HandleSendMessage(this, ref sendMessage);
                }

                CommunicationPrimitives.SendMessage(this.tcpClient, sendMessage, this.sslStream);
            }
        }

        private void ReceiveMessages()
        {
            if (this.tcpClient.Available > 0)
            {
                /*
                 * When a message has been received, process it.
                 */
                var message = CommunicationPrimitives.GetMessage(this.safeNetworkStream, this.sslStream);
                this.LogDebug("Received a message of size: " + message.MessageLength);
                this.PrehandleReceivedMessage(message);
                this.HandleReceivedMessage(message);
            }
        }

        private void PrehandleReceivedMessage(Message message)
        {
            foreach (var handler in this.handlers)
            {
                if (handler.PreHandleReceivedMessage(this, ref message))
                {
                    break;
                }
            }
        }

        private void HandleReceivedMessage(Message message)
        {
            foreach (var handler in this.handlers)
            {
                if (handler.HandleReceivedMessage(this, message))
                {
                    break;
                }
            }
        }

        private void TickHandlers()
        {
            foreach (var handler in this.handlers)
            {
                handler.Tick(this);
            }
        }

        private void HandleException(Exception exception)
        {
            this.LogDebug("Exception: " + exception.Message);
            this.LogDebug("Stacktrace: " + exception.StackTrace);
            foreach (var exceptionHandler in this.exceptionHandlers)
            {
                exceptionHandler.HandleException(exception);
            }
        }
        #endregion
    }
}
