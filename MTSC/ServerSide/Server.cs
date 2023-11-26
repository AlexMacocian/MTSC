using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MTSC.Common;
using MTSC.Exceptions;
using MTSC.ServerSide.BackgroundServices;
using MTSC.ServerSide.Handlers;
using MTSC.ServerSide.Listeners;
using MTSC.ServerSide.Schedulers;
using MTSC.ServerSide.UsageMonitors;
using Slim;
using Slim.Integration.ServiceCollection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// TODO: Setup basic background services
namespace MTSC.ServerSide
{
    /// <summary>
    /// Main server class.
    /// </summary>
    public sealed class Server
    {
        #region Fields
        private CancellationToken cancellationToken;
        private bool initialized;
        private X509Certificate2 certificate;
        private readonly BackgroundServicesHolder backgroundServicesHolder;
        private readonly ProducerConsumerQueue<ClientData> addQueue = new();
        private readonly List<ClientData> clients = new();
        private readonly List<ClientData> toRemove = new();
        private readonly List<IHandler> handlers = new();
        private readonly List<IExceptionHandler> exceptionHandlers = new();
        private readonly List<IServerUsageMonitor> serverUsageMonitors = new();
        private readonly ProducerConsumerQueue<(ClientData, byte[])> messageOutQueue = new();
        private ILogger logger = null;
        #endregion
        #region Private Properties
        private IConsumerQueue<ClientData> ConsumerClientQueue { get => this.addQueue; }
        private IProducerQueue<ClientData> ProducerClientQueue { get => this.addQueue; }
        private IConsumerQueue<(ClientData, byte[])> ConsumerMessageOutQueue { get => this.messageOutQueue; }
        #endregion
        #region Public Properties
        public IListener Listener { get; set; } = new TcpSocketListener();
        /// <summary>
        /// Timeout for socket operations on clients
        /// </summary>
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromMilliseconds(50);
        /// <summary>
        /// Client handling scheduler
        /// </summary>
        public IScheduler Scheduler { get; set; } = new ParallelScheduler();
        /// <summary>
        /// Queue of destinations and messages to be processed
        /// </summary>
        public IProducerQueue<(ClientData, byte[])> MessageOutQueue { get => this.messageOutQueue; }
        /// <summary>
        /// Duration until ssl authentication gives up during the authentication process
        /// </summary>
        public TimeSpan SslAuthenticationTimeout { get; set; } = TimeSpan.FromSeconds(1);
        /// <summary>
        /// SSL supported protocols.
        /// </summary>
        public SslProtocols SslProtocols { get; set; } = SslProtocols.None;
        /// <summary>
        /// Remote certificate validation callback.
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; } = new RemoteCertificateValidationCallback((o, e, s, p) => true);
        /// <summary>
        /// Local certificate selection callback.
        /// </summary>
        public LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get; set; } = null;
        /// <summary>
        /// SSL Encryption policy.
        /// </summary>
        public EncryptionPolicy EncryptionPolicy { get; set; } = EncryptionPolicy.RequireEncryption;
        /// <summary>
        /// Server port.
        /// </summary>
        public int Port { get; set; } = 80;
        /// <summary>
        /// IPAddress used by the server.
        /// </summary>
        public IPAddress IPAddress { get; set; } = IPAddress.Any;
        /// <summary>
        /// Returns the state of the server.
        /// </summary>
        public bool Running => this.Listener?.Active is true;
        /// <summary>
        /// If set to true, requests client certificates.
        /// </summary>
        public bool RequestClientCertificate { get; set; } = true;
        /// <summary>
        /// If set to true, logs contents of the received messages.
        /// </summary>
        public bool LogMessageContents { get; set; } = false;
        /// <summary>
        /// List of clients currently connected to the server.
        /// </summary>
        public IReadOnlyCollection<ClientData> Clients { get => this.clients.AsReadOnly(); }
        /// <summary>
        /// <see cref="IServiceManager"/> for configuring and retrieving services. Will be populated at server startup from the <see cref="ServiceCollection"/>.
        /// </summary>
        public IServiceManager ServiceManager { get; private set; } = new ServiceManager();
        /// <summary>
        /// <see cref="IServiceCollection"/> used to populate the <see cref="IServiceManager"/> at server startup.
        /// </summary>
        public IServiceCollection ServiceCollection { get; } = new ServiceCollection();
        #endregion
        #region Constructors
        /// <summary>
        /// Creates an instance of server with default values.
        /// </summary>
        public Server()
        {
            this.ServiceCollection.AddSingleton(this);
            this.backgroundServicesHolder = new(this);
        }
        /// <summary>
        /// Creates an instance of server.
        /// </summary>
        /// <param name="port">Port to be used by server.</param>
        public Server(int port)
        {
            this.Port = port;
            this.ServiceCollection.AddSingleton(this);
            this.backgroundServicesHolder = new(this);
        }
        /// <summary>
        /// Creates an instance of server.
        /// </summary>
        /// <param name="certificate">Certificate for SSL.</param>
        /// <param name="port">Port to be used by the server.</param>
        public Server(X509Certificate2 certificate, int port)
        {
            this.certificate = certificate;
            this.Port = port;
            this.ServiceCollection.AddSingleton(this);
            this.backgroundServicesHolder = new(this);
        }
        /// <summary>
        /// Creates an instance of server.
        /// </summary>
        /// <param name="ipAddress">IPAddress to be used by the server.</param>
        /// <param name="port">Port to be used by the server.</param>
        public Server(IPAddress ipAddress, int port)
        {
            this.IPAddress = ipAddress;
            this.Port = port;
            this.ServiceCollection.AddSingleton(this);
            this.backgroundServicesHolder = new(this);
        }
        /// <summary>
        /// Creates an instance of server.
        /// </summary>
        /// <param name="ipAddress">IPAddress to be used by the server.</param>
        /// <param name="certificate">Certificate for SSL.</param>
        /// <param name="port">Port to be used by the server.</param>
        public Server(X509Certificate2 certificate, IPAddress ipAddress, int port)
        {
            this.certificate = certificate;
            this.Port = port;
            this.IPAddress = ipAddress;
            this.ServiceCollection.AddSingleton(this);
            this.backgroundServicesHolder = new(this);
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Adds a <see cref="BackgroundServiceBase"/> to the server.
        /// </summary>
        /// <param name="interval">Interval between ticks.</param>
        /// <typeparam name="TBackgroundService">Type of the background service.</typeparam>
        /// <returns>This <see cref="Server"/> object.</returns>
        public Server AddBackgroundService<TBackgroundService>(TimeSpan interval)
            where TBackgroundService : BackgroundServiceBase
        {
            this.backgroundServicesHolder.RegisterBackgroundService<TBackgroundService>(interval);
            return this;
        }
        /// <summary>
        /// Sets the <see cref="ReadTimeout"/> property.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Server WithReadTimeout(TimeSpan timeout)
        {
            this.ReadTimeout = timeout;
            return this;
        }
        /// <summary>
        /// Sets the scheduler of the server
        /// </summary>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public Server SetScheduler(IScheduler scheduler)
        {
            this.Scheduler = scheduler;
            return this;
        }
        /// <summary>
        /// Ssl authentication timeout
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public Server WithSslAuthenticationTimeout(TimeSpan timeout)
        {
            this.SslAuthenticationTimeout = timeout;
            return this;
        }
        /// <summary>
        /// Requests that the client provides a certificate.
        /// </summary>
        /// <param name="requestCertificate"></param>
        /// <returns>This server object</returns>
        public Server WithClientCertificate(bool requestCertificate)
        {
            this.RequestClientCertificate = requestCertificate;
            return this;
        }
        /// <summary>
        /// Sets the <see cref="LogMessageContents"/> property.
        /// </summary>
        /// <param name="logMessageContents">Value to be set.</param>
        /// <returns><see cref="Server"/>.</returns>
        public Server WithLoggingMessageContents(bool logMessageContents)
        {
            this.LogMessageContents = logMessageContents;
            return this;
        }
        /// <summary>
        /// Sets the server supported ssl protocols
        /// </summary>
        /// <param name="sslProtocols">Ssl protocols.</param>
        /// <returns>This server object.</returns>
        public Server WithSslProtocols(SslProtocols sslProtocols)
        {
            this.SslProtocols = sslProtocols;
            return this;
        }
        /// <summary>
        /// Sets the server certificate.
        /// </summary>
        /// <param name="certificate2">Certificate to be used for SSL.</param>
        /// <returns>This server object.</returns>
        public Server WithCertificate(X509Certificate2 certificate2)
        {
            this.certificate = certificate2;
            return this;
        }
        /// <summary>
        /// Sets the remote certificate validation callback.
        /// </summary>
        /// <param name="remoteCertificateValidationCallback">RemoteCertificateValidationCallback</param>
        /// <returns>This server object.</returns>
        public Server WithRemoteCertificateValidation(RemoteCertificateValidationCallback remoteCertificateValidationCallback)
        {
            this.RemoteCertificateValidationCallback = remoteCertificateValidationCallback;
            return this;
        }
        /// <summary>
        /// Sets the local certificate selection callback.
        /// </summary>
        /// <param name="localCertificateSelectionCallback">LocalCertificateSelectionCallback</param>
        /// <returns>This server object.</returns>
        public Server WithLocalCertificateSelection(LocalCertificateSelectionCallback localCertificateSelectionCallback)
        {
            this.LocalCertificateSelectionCallback = localCertificateSelectionCallback;
            return this;
        }
        /// <summary>
        /// Sets the encryption policy for SSL streams.
        /// </summary>
        /// <param name="encryptionPolicy">EncryptionPolicy</param>
        /// <returns>This server object.</returns>
        public Server WithEncryptionPolicy(EncryptionPolicy encryptionPolicy)
        {
            this.EncryptionPolicy = encryptionPolicy;
            return this;
        }
        /// <summary>
        /// Sets the port to the specified value.
        /// </summary>
        /// <param name="port">Port value.</param>
        /// <returns>This server instance.</returns>
        public Server SetPort(int port)
        {
            this.Port = port;
            return this;
        }
        /// <summary>
        /// Sets the <see cref="IPAddress"/> of the server.
        /// </summary>
        /// <param name="iPAddress">Address used by the server.</param>
        /// <returns>This server object.</returns>
        public Server SetIPAddress(IPAddress iPAddress)
        {
            this.IPAddress = iPAddress;
            return this;
        }
        /// <summary>
        /// Adds a <see cref="IHandler"/> to the server.
        /// </summary>
        /// <param name="handler">Handler to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddHandler(IHandler handler)
        {
            this.handlers.Add(handler);
            return this;
        }
        /// <summary>
        /// Adds an <see cref="IExceptionHandler"/> to the server.
        /// </summary>
        /// <param name="handler">Handler to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddExceptionHandler(IExceptionHandler handler)
        {
            this.exceptionHandlers.Add(handler);
            return this;
        }
        /// <summary>
        /// Adds a <see cref="IServerUsageMonitor"/> to the server.
        /// </summary>
        /// <param name="serverUsageMonitor">Monitor to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddServerUsageMonitor(IServerUsageMonitor serverUsageMonitor)
        {
            this.serverUsageMonitors.Add(serverUsageMonitor);
            return this;
        }
        /// <summary>
        /// Get handler of provided type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetHandler<T>() where T : class
        {
            foreach(var handler in this.handlers)
            {
                if(handler is T)
                {
                    return handler as T;
                }
            }

            return null;
        }
        /// <summary>
        /// Get exception handler of provided type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetExceptionHandler<T>() where T : class
        {
            foreach (var exceptionHandler in this.exceptionHandlers)
            {
                if (exceptionHandler is T)
                {
                    return exceptionHandler as T;
                }
            }

            return null;
        }
        /// <summary>
        /// Get server usage monitor of provided type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetServerUsageMonitor<T>() where T : class
        {
            foreach (var serverMonitor in this.serverUsageMonitors)
            {
                if(serverMonitor is T)
                {
                    return serverMonitor as T;
                }
            }

            return null;
        }
        /// <summary>
        /// Queues a message to be sent.
        /// </summary>
        /// <param name="target">Target client.</param>
        /// <param name="message">Message to be sent.</param>
        public void QueueMessage(ClientData target, byte[] message)
        {
            (this.MessageOutQueue as IProducerQueue<(ClientData, byte[])>).Enqueue((target, message));
        }
        /// <summary>
        /// Adds a message to be logged by the associated loggers.
        /// </summary>
        /// <remarks>
        /// This method will not log anything if no <see cref="ILogger"/> or <see cref="ILogger{Server}"/> can be resolved by the <see cref="IServiceManager"/>.
        /// </remarks>
        /// <param name="log">Message to be logged</param>
        public void Log(string log)
        {
            this.logger?.LogInformation(log);
        }
        /// <summary>
        /// Adds a debug message to be logged by the associated loggers.
        /// </summary>
        /// <remarks>
        /// This method will not log anything if no <see cref="ILogger"/> or <see cref="ILogger{TCategoryName}"/> can be resolved by the <see cref="IServiceManager"/>.
        /// </remarks>
        /// <param name="debugMessage">Debug message to be logged</param>
        public void LogDebug(string debugMessage)
        {
            this.logger?.LogDebug(debugMessage);
        }
        /// <summary>
        /// Blocking method. Runs the server on the current thread.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token used to cancel the server.</param>
        public void Run(CancellationToken cancellationToken = default)
        {
            lock (this)
            {
                if (this.Running)
                {
                    return;
                }

                /*
                 * Run only once when the server is first initializing.
                 * This gives the handlers an opportunity to schedule tasks, register services, etc.
                 */
                if (this.initialized is false)
                {
                    this.initialized = true;
                    this.ServiceManager = this.ServiceCollection.BuildSlimServiceProvider(this.ServiceManager) as IServiceManager;

                    foreach (var toBeRunOnInitialization in this.handlers.OfType<IRunOnInitialization>())
                    {
                        toBeRunOnInitialization.OnInitialization(this);
                    }

                    // Try to get a logger, in case it exists. If not, keep it null.
                    try
                    {
                        this.logger = this.ServiceManager.GetService<ILogger<Server>>();
                    }
                    catch
                    {
                        try
                        {
                            this.logger = this.ServiceManager.GetService<ILogger>();
                        }
                        catch
                        {
                        }
                    }
                }

                this.Listener?.Stop();
                this.Listener.Initialize(this.Port, this.IPAddress);
                this.Listener.Start();
                this.cancellationToken = cancellationToken;
                this.Log("Server starting on: " + this.Listener.LocalEndpoint.ToString());
                foreach (var toBeRunOnStartup in this.handlers.OfType<IRunOnStartup>())
                {
                    toBeRunOnStartup.OnStartup(this);
                }

                /*
                 * Initialize background services.
                 */
                this.backgroundServicesHolder.Initialize();
            }

            DateTime startLoopTime;
            while (this.Listener?.Active is true)
            {
                startLoopTime = DateTime.Now;
                
                /*
                 * Check cancellationToken. If cancellation has been requested, stop the loop
                 * and let the cleanup process deal with the server resources.
                 */
                if (this.cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                /*
                 * Check the client states. If a client is disconnected, 
                 * remove it from the list of clients.
                 */
                try
                {
                    this.CheckAndRemoveInactiveClients();
                }
                catch (Exception e)
                {
                    this.HandleException(e);
                }
                /*
                 * Check if the server has any pending connections.
                 * If it has a new connection, process it.
                 */
                try
                {
                    while (this.Listener.Pending())
                    {
                        var client = this.Listener.AcceptSocket();
                        var clientStruct = new ClientData(client);
                        new TaskFactory().StartNew(() => this.AcceptClient(clientStruct), this.cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                    }
                }
                catch (Exception e)
                {
                    this.HandleException(e);
                }
                /*
                 * Add all accepted clients to the list
                 */
                while (this.ConsumerClientQueue.TryDequeue(out var client))
                {
                    this.Log("Accepted new connection: " + client.Socket.RemoteEndPoint.ToString());
                    this.clients.Add(client);
                    foreach (var handler in this.handlers)
                    {
                        if (handler.HandleClient(this, client))
                        {
                            break;
                        }
                    }

                    try
                    {
                        CommunicationPrimitives.LoopRead(client, this.MessageReceived);
                    }
                    catch (Exception e)
                    {
                        this.HandleException(e);
                    }
                }

                /*
                 * Call the scheduler to handle all received messages and distribute them to the handlers
                 */

                this.Scheduler.ScheduleHandling(
                    this.clients
                        .Where(client => client.ToBeRemoved == false)
                        .Select(client => (client, (client as IQueueHolder<Message>).ConsumerQueue))
                        .ToList(),
                    this.HandleClientMessages);

                /*
                 * Iterate through all the handlers, running periodic operations.
                 */
                foreach(var handler in this.handlers)
                {
                    this.TickHandler(handler);
                }

                /*
                 * Tick background services
                 */
                try
                {
                    this.backgroundServicesHolder.Tick();
                }
                catch(Exception e)
                {
                    this.HandleException(e);
                }

                /*
                 * Check if there are messages queued to be sent.
                 */
                this.SendQueuedMessages();

                /*
                 * Call the usage monitors and let them scale or determine current resource usage.
                 */
                foreach (var usageMonitor in this.serverUsageMonitors)
                {
                    try
                    {
                        usageMonitor.Tick(this);
                    }
                    catch(Exception e)
                    {
                        this.HandleException(e);
                    }
                }
            }

            this.Listener?.Stop();
            foreach (var client in this.Clients)
            {
                try
                {
                    client?.Dispose();
                }
                catch (Exception e)
                {
                    this.HandleException(e);
                }
            }
        }
        /// <summary>
        /// Runs the server async.
        /// </summary>
        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() => this.Run(cancellationToken));
        }
        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            this.Listener?.Stop();
        }
        #endregion
        #region Private Methods
        private void SendQueuedMessages()
        {
            while (this.ConsumerMessageOutQueue.TryDequeue(out var tuple))
            {
                (var client, var bytes) = tuple;

                try 
                {
                    var sendMessage = CommunicationPrimitives.BuildMessage(bytes);
                    for (var i = this.handlers.Count - 1; i >= 0; i--)
                    {
                        var handler = this.handlers[i];
                        handler.HandleSendMessage(this, client, ref sendMessage);
                    }

                    CommunicationPrimitives.SendMessage(sendMessage, client.SafeNetworkStream, client.SslStream);
                    (client as IActiveClient).UpdateLastActivity();
                    this.LogDebug("Sent message to " + client.Socket.RemoteEndPoint.ToString() +
                        "\nMessage length: " + sendMessage.MessageLength);
                }
                catch(Exception e)
                {
                    this.HandleException(e);
                }
            }
        }
        private void CheckAndRemoveInactiveClients()
        {
            foreach (var client in this.Clients)
            {
                if (!client.Socket.Connected || client.ToBeRemoved)
                {
                    this.toRemove.Add(client);
                }
            }

            foreach (var client in this.toRemove)
            {
                try
                {
                    foreach (var handler in this.handlers)
                    {
                        handler.ClientRemoved(this, client);
                    }

                    this.LogDebug("Client removed: " + client.Socket?.RemoteEndPoint?.ToString());
                    client.Dispose();
                }
                catch(Exception e)
                {
                    this.HandleException(e);
                }

                this.clients.Remove(client);
            }

            this.toRemove.Clear();
        }
        private void TickHandler(IHandler handler)
        {
            try
            {
                handler.Tick(this);
            }
            catch (Exception e)
            {
                this.HandleException(e);
            }
        }
        private void MessageReceived(ClientData client, Message message)
        {
            (client as IQueueHolder<Message>).Enqueue(message);
            this.LogDebug($"Received message from {client.Socket.RemoteEndPoint as IPEndPoint} Message length: {message.MessageLength}");
            if (this.LogMessageContents)
            {
                this.LogDebug(Encoding.UTF8.GetString(message.MessageBytes));
            }
        }
        private void HandleClientMessages(ClientData client, IConsumerQueue<Message> messages)
        {
            while(messages.TryDequeue(out var message))
            {
                if (client.Affinity is null)
                {
                    this.HandleClientMessage(client, message);
                }
                else
                {
                    this.AffinityHandleClientMessage(client, message);
                }
            }
        }
        private void AffinityHandleClientMessage(ClientData client, Message message)
        {
            var handler = client.Affinity;
            try
            {
                handler.PreHandleReceivedMessage(this, client, ref message);
            }
            catch (Exception e)
            {
                this.HandleException(e);
            }

            try
            {
                handler.HandleReceivedMessage(this, client, message);
            }
            catch (Exception e)
            {
                this.HandleException(e);
            }
        }
        private void HandleClientMessage(ClientData client, Message message)
        {
            foreach (var handler in this.handlers)
            {
                try
                {
                    if (handler.PreHandleReceivedMessage(this, client, ref message))
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    this.HandleException(e);
                }
            }

            foreach (var handler in this.handlers)
            {
                try
                {
                    if (handler.HandleReceivedMessage(this, client, message))
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    this.HandleException(e);
                }
            }
        }
        private void AcceptClient(ClientData client)
        {
            try
            {
                if (this.certificate != null)
                {
                    SslStream sslStream = new(client.SafeNetworkStream,
                        false,
                        this.RemoteCertificateValidationCallback,
                        this.LocalCertificateSelectionCallback,
                        this.EncryptionPolicy);
                    client.SslStream = sslStream;

                    if (sslStream.AuthenticateAsServerAsync(this.certificate, this.RequestClientCertificate, this.SslProtocols, false).Wait(this.SslAuthenticationTimeout))
                    {
                        /*
                         * Client authenticated in the alloted time
                         */
                        this.ProducerClientQueue.Enqueue(client);
                    }
                    else
                    {
                        client.Dispose();
                    }
                }
                else
                {
                    this.ProducerClientQueue.Enqueue(client);
                }
            }
            catch (Exception e)
            {
                this.HandleException(e);
                client.Dispose();
            }
        }
        private void HandleException(Exception exception)
        {
            this.Log($"Encountered {exception.GetType().Name}");
            this.LogDebug(exception.ToString());
            foreach (var exceptionHandler in this.exceptionHandlers)
            {
                if (exceptionHandler.HandleException(exception))
                {
                    break;
                }
            }
        }
        #endregion
    }
}
