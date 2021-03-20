using MTSC.Common;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.ServerSide.Handlers;
using MTSC.ServerSide.Resources;
using MTSC.ServerSide.Schedulers;
using MTSC.ServerSide.UsageMonitors;
using Slim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MTSC.ServerSide
{
    /// <summary>
    /// Basic server class to handle TCP connections.
    /// </summary>
    public sealed class Server
    {
        #region Fields
        private bool running;
        private X509Certificate2 certificate;
        private TcpListener listener;
        private readonly ProducerConsumerQueue<ClientData> addQueue = new ProducerConsumerQueue<ClientData>();
        private readonly List<ClientData> clients = new List<ClientData>();
        private readonly List<ClientData> toRemove = new List<ClientData>();
        private readonly List<IHandler> handlers = new List<IHandler>();
        private readonly List<ILogger> loggers = new List<ILogger>();
        private readonly List<IExceptionHandler> exceptionHandlers = new List<IExceptionHandler>();
        private readonly List<IServerUsageMonitor> serverUsageMonitors = new List<IServerUsageMonitor>();
        private readonly ProducerConsumerQueue<(ClientData, byte[])> messageOutQueue = new ProducerConsumerQueue<(ClientData, byte[])>();
        private readonly IServiceManager serviceManager = new ServiceManager();
        #endregion
        #region Private Properties
        private IConsumerQueue<ClientData> ConsumerClientQueue { get => addQueue; }
        private IProducerQueue<ClientData> ProducerClientQueue { get => addQueue; }
        private IConsumerQueue<(ClientData, byte[])> ConsumerMessageOutQueue { get => messageOutQueue; }
        #endregion
        #region Public Properties
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
        public IProducerQueue<(ClientData, byte[])> MessageOutQueue { get => messageOutQueue; }
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
        public bool Running { get => listener != null; }
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
        /// <see cref="IServiceManager"/> for configuring and retrieving services.
        /// </summary>
        public IServiceManager ServiceManager { get => this.serviceManager; }
        #endregion
        #region Constructors
        /// <summary>
        /// Creates an instance of server with default values.
        /// </summary>
        public Server()
        {
        }
        /// <summary>
        /// Creates an instance of server.
        /// </summary>
        /// <param name="port">Port to be used by server.</param>
        public Server(int port)
        {
            this.Port = port;
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
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Adds a service with transient lifetime.
        /// </summary>
        /// <returns>This server object.</returns>
        public Server AddTransientService<TInterface, TService>()
            where TService : TInterface
            where TInterface : class
        {
            this.serviceManager.RegisterTransient<TInterface, TService>();
            return this;
        }
        /// <summary>
        /// Adds a service with transient lifetime. Registers the service for all the interfaces it implements.
        /// </summary>
        /// <returns>This server object.</returns>
        public Server AddTransientService<TService>()
            where TService : class
        {
            this.serviceManager.RegisterTransient<TService>();
            return this;
        }
        /// <summary>
        /// Adds a service with transient lifetime.
        /// </summary>
        /// <returns>This server object.</returns>
        public Server AddTransientService<TInterface, TService>(Func<Slim.IServiceProvider, TService> serviceFactory)
            where TService : TInterface
            where TInterface : class
        {
            this.serviceManager.RegisterTransient<TInterface, TService>(serviceFactory);
            return this;
        }
        /// <summary>
        /// Adds a service with transient lifetime. Registers the service for all the interfaces it implements.
        /// </summary>
        /// <returns>This server object.</returns>
        public Server AddTransientService<TService>(Func<Slim.IServiceProvider, TService> serviceFactory)
            where TService : class
        {
            this.serviceManager.RegisterTransient<TService>(serviceFactory);
            return this;
        }
        /// <summary>
        /// Adds a service with singleton lifetime.
        /// </summary>
        /// <returns>This server object.</returns>
        public Server AddSingletonService<TInterface, TService>()
            where TService : TInterface
            where TInterface : class
        {
            this.serviceManager.RegisterSingleton<TInterface, TService>();
            return this;
        }
        /// <summary>
        /// Adds a service with singleton lifetime. Registers the service for all the interfaces it implements.
        /// </summary>
        /// <returns>This server object.</returns>
        public Server AddSingletonService<TService>()
            where TService : class
        {
            this.serviceManager.RegisterSingleton<TService>();
            return this;
        }
        /// <summary>
        /// Adds a service with singleton lifetime.
        /// </summary>
        /// <returns>This server object.</returns>
        public Server AddSingletonService<TInterface, TService>(Func<Slim.IServiceProvider, TService> serviceFactory)
            where TService : TInterface
            where TInterface : class
        {
            this.serviceManager.RegisterSingleton<TInterface, TService>(serviceFactory);
            return this;
        }
        /// <summary>
        /// Adds a service with singleton lifetime. Registers the service for all the interfaces it implements.
        /// </summary>
        /// <returns>This server object.</returns>
        public Server AddSingletonService<TService>(Func<Slim.IServiceProvider, TService> serviceFactory)
            where TService : class
        {
            this.serviceManager.RegisterSingleton<TService>(serviceFactory);
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
            RequestClientCertificate = requestCertificate;
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
        /// Adds a <see cref="ILogger"/> to the server.
        /// </summary>
        /// <param name="logger">Logger to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddLogger(ILogger logger)
        {
            this.loggers.Add(logger);
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
        /// Gets a required service.
        /// </summary>
        /// <typeparam name="T">Type of the service used during registration.</typeparam>
        /// <returns></returns>
        public T GetService<T>()
            where T : class
        {
            return this.serviceManager.GetService<T>();
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
        /// Get logger of provided type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetLogger<T>() where T : class
        {
            foreach (var logger in this.loggers)
            {
                if (logger is T)
                {
                    return logger as T;
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
            (MessageOutQueue as IProducerQueue<(ClientData, byte[])>).Enqueue((target, message));
        }
        /// <summary>
        /// Adds a message to be logged by the associated loggers.
        /// </summary>
        /// <param name="log">Message to be logged</param>
        public void Log(string log)
        {
            foreach (ILogger logger in this.loggers)
            {
                if (logger.Log(log))
                {
                    break;
                }
            }
        }
        /// <summary>
        /// Adds a debug message to be logged by the associated loggers.
        /// </summary>
        /// <param name="debugMessage">Debug message to be logged</param>
        public void LogDebug(string debugMessage)
        {
            foreach (ILogger logger in this.loggers)
            {
                if (logger.LogDebug(debugMessage))
                {
                    break;
                }
            }
        }
        /// <summary>
        /// Blocking method. Runs the server on the current thread.
        /// </summary>
        public void Run()
        {
            this.listener?.Stop();
            this.listener = new TcpListener(this.IPAddress, this.Port);
            this.listener.Start();
            this.running = true;
            this.Log("Server started on: " + this.listener.LocalEndpoint.ToString());
            foreach(var toBeRunOnStartup in this.handlers.OfType<IRunOnStartup>())
            {
                toBeRunOnStartup.OnStartup(this);
            }

            this.serviceManager.RegisterServiceManager();
            this.serviceManager.RegisterSingleton<Server, Server>(sp => this);
            DateTime startLoopTime;
            while (running)
            {
                startLoopTime = DateTime.Now;
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
                    foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                    {
                        if (exceptionHandler.HandleException(e))
                        {
                            break;
                        }
                    }
                }
                /*
                 * Check and gather messages from clients and place them in their queues.
                 */
                this.CheckAndGatherMessages();
                /*
                 * Check if the server has any pending connections.
                 * If it has a new connection, process it.
                 */
                try
                {
                    while (this.listener.Pending())
                    {
                        var tcpClient = this.listener.AcceptTcpClient();
                        var clientStruct = new ClientData(tcpClient);
                        Task.Run(() => this.AcceptClient(clientStruct));
                    }
                }
                catch (Exception e)
                {
                    foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                    {
                        if (exceptionHandler.HandleException(e))
                        {
                            break;
                        }
                    }
                }
                /*
                 * Add all accepted clients to the list
                 */
                while(this.ConsumerClientQueue.TryDequeue(out var client)) 
                {
                    this.Log("Accepted new connection: " + client.TcpClient.Client.RemoteEndPoint.ToString());
                    this.clients.Add(client);
                    foreach (IHandler handler in this.handlers)
                    {
                        if (handler.HandleClient(this, client))
                        {
                            break;
                        }
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
                foreach(IHandler handler in this.handlers)
                {
                    this.TickHandler(handler);
                }

                /*
                 * Check if there are messages queued to be sent.
                 */
                this.SendQueuedMessages();

                /*
                 * Call the usage monitors and let them scale or determine current resource usage.
                 */
                foreach (IServerUsageMonitor usageMonitor in this.serverUsageMonitors)
                {
                    try
                    {
                        usageMonitor.Tick(this);
                    }
                    catch(Exception e)
                    {
                        foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                        {
                            if (exceptionHandler.HandleException(e))
                            {
                                break;
                            }
                        }
                    }
                }
            }
            this.listener.Stop();
            foreach (var client in this.Clients)
            {
                try
                {
                    client?.SslStream?.Dispose();
                    client?.TcpClient?.Dispose();
                }
                catch (Exception e)
                {
                    foreach (var handler in this.exceptionHandlers)
                    {
                        handler.HandleException(e);
                    }
                }
            }
            this.listener = null;
        }
        /// <summary>
        /// Runs the server async.
        /// </summary>
        public Task RunAsync()
        {
            return Task.Run(Run);
        }
        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            if (running)
            {
                running = false;
            }
        }
        #endregion
        #region Private Methods
        private void SendQueuedMessages()
        {
            while (this.ConsumerMessageOutQueue.TryDequeue(out var tuple))
            {
                (var client, var bytes) = tuple;
                if (client.TcpClient.Available > 0)
                {
                    /*
                     * Don't send while client is still sending
                     */
                    continue;
                }
                try 
                {
                    Message sendMessage = CommunicationPrimitives.BuildMessage(bytes);
                    for (int i = this.handlers.Count - 1; i >= 0; i--)
                    {
                        IHandler handler = handlers[i];
                        handler.HandleSendMessage(this, client, ref sendMessage);
                    }
                    CommunicationPrimitives.SendMessage(client.TcpClient, sendMessage, client.SslStream);
                    (client as IActiveClient).UpdateLastActivity();
                    this.LogDebug("Sent message to " + client.TcpClient.Client.RemoteEndPoint.ToString() +
                        "\nMessage length: " + sendMessage.MessageLength);
                }
                catch(Exception e)
                {
                    foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                    {
                        if (exceptionHandler.HandleException(e))
                        {
                            break;
                        }
                    }
                }
            }
        }
        private void CheckAndRemoveInactiveClients()
        {
            foreach (ClientData client in this.Clients)
            {
                if (!client.TcpClient.Connected || client.ToBeRemoved)
                {
                    this.toRemove.Add(client);
                }
            }
            foreach (ClientData client in this.toRemove)
            {
                try
                {
                    foreach (IHandler handler in this.handlers)
                    {
                        handler.ClientRemoved(this, client);
                    }
                    this.LogDebug("Client removed: " + client.TcpClient?.Client?.RemoteEndPoint?.ToString());
                    client.Dispose();
                }
                catch(Exception e)
                {
                    foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                    {
                        if (exceptionHandler.HandleException(e))
                        {
                            break;
                        }
                    }
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
                foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                {
                    if (exceptionHandler.HandleException(e))
                    {
                        break;
                    }
                }
            }
        }
        private void CheckAndGatherMessages()
        {
            foreach(var client in this.Clients)
            {
                if (client.TcpClient.Available > 0 && !(client as IActiveClient).ReadingData)
                {
                    (client as IActiveClient).ReadingData = true;
                    Task.Run(() =>
                    {
                        try
                        {
                            var timeout = this.ReadTimeout;
                            if (client.TcpClient.Available < 1000)
                            {
                                timeout = TimeSpan.FromMilliseconds(50);
                            }

                            var message = CommunicationPrimitives.GetMessage(client, timeout);
                            (client as IQueueHolder<Message>).Enqueue(message);
                            this.LogDebug($"Received message from {(client.TcpClient.Client.RemoteEndPoint as IPEndPoint)} Message length: {message.MessageLength}");
                            if (this.LogMessageContents)
                            {
                                this.LogDebug(Encoding.UTF8.GetString(message.MessageBytes));
                            }

                            (client as IActiveClient).ReadingData = false;
                        }
                        catch (Exception)
                        {
                            client.ToBeRemoved = true;
                        }
                    });
                }
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
                foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                {
                    if (exceptionHandler.HandleException(e))
                    {
                        break;
                    }
                }
            }
            try
            {
                handler.HandleReceivedMessage(this, client, message);
            }
            catch (Exception e)
            {
                foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                {
                    if (exceptionHandler.HandleException(e))
                    {
                        break;
                    }
                }
            }
        }
        private void HandleClientMessage(ClientData client, Message message)
        {
            foreach (IHandler handler in this.handlers)
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
                    foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                    {
                        if (exceptionHandler.HandleException(e))
                        {
                            break;
                        }
                    }
                }
            }
            foreach (IHandler handler in this.handlers)
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
                    foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                    {
                        if (exceptionHandler.HandleException(e))
                        {
                            break;
                        }
                    }
                }
            }
        }
        private void AcceptClient(ClientData client)
        {
            try
            {
                if (this.certificate != null)
                {
                    SslStream sslStream = new SslStream(client.SafeNetworkStream,
                        false,
                        this.RemoteCertificateValidationCallback,
                        this.LocalCertificateSelectionCallback,
                        this.EncryptionPolicy);
                    client.SslStream = sslStream;

                    if(sslStream.AuthenticateAsServerAsync(this.certificate, this.RequestClientCertificate, this.SslProtocols, false).Wait(SslAuthenticationTimeout))
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
                foreach (IExceptionHandler exceptionHandler in this.exceptionHandlers)
                {
                    if (exceptionHandler.HandleException(e))
                    {
                        break;
                    }
                }
                client.Dispose();
            }
        }
        #endregion
    }
}
