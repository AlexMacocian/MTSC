using MTSC.Common;
using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.ServerSide.Handlers;
using MTSC.ServerSide.Resources;
using MTSC.ServerSide.Schedulers;
using MTSC.ServerSide.UsageMonitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace MTSC.ServerSide
{
    /// <summary>
    /// Basic server class to handle TCP connections.
    /// </summary>
    public sealed class Server
    {
        #region Fields
        bool running;
        X509Certificate2 certificate;
        TcpListener listener;
        ProducerConsumerQueue<ClientData> addQueue = new ProducerConsumerQueue<ClientData>();
        List<ClientData> clients = new List<ClientData>();
        List<ClientData> toRemove = new List<ClientData>();
        List<IHandler> handlers = new List<IHandler>();
        List<ILogger> loggers = new List<ILogger>();
        List<IExceptionHandler> exceptionHandlers = new List<IExceptionHandler>();
        List<IServerUsageMonitor> serverUsageMonitors = new List<IServerUsageMonitor>();
        ProducerConsumerQueue<(ClientData, byte[])> messageOutQueue = new ProducerConsumerQueue<(ClientData, byte[])>();
        #endregion
        #region Private Properties
        private IConsumerQueue<ClientData> _ConsumerClientQueue { get => addQueue; }
        private IProducerQueue<ClientData> _ProducerClientQueue { get => addQueue; }
        private IConsumerQueue<(ClientData, byte[])> _ConsumerMessageOutQueue { get => messageOutQueue; }
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
        /// Returns the state of the server.
        /// </summary>
        public bool Running { get => listener != null; }
        public bool RequestClientCertificate { get; set; } = true;
        /// <summary>
        /// List of clients currently connected to the server.
        /// </summary>
        public IReadOnlyCollection<ClientData> Clients { get => clients.AsReadOnly(); }
        /// <summary>
        /// Dictionary of resources
        /// </summary>
        public Dictionary<Type, IResource> Resources { get; } = new Dictionary<Type, IResource>();
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
        #endregion
        #region Public Methods
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
        public Server WithResource(IResource resource)
        {
            Resources[resource.GetType()] = resource;
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
        /// Adds a <see cref="IHandler"/> to the server.
        /// </summary>
        /// <param name="handler">Handler to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddHandler(IHandler handler)
        {
            handlers.Add(handler);
            return this;
        }
        /// <summary>
        /// Adds a <see cref="ILogger"/> to the server.
        /// </summary>
        /// <param name="logger">Logger to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddLogger(ILogger logger)
        {
            loggers.Add(logger);
            return this;
        }
        /// <summary>
        /// Adds an <see cref="IExceptionHandler"/> to the server.
        /// </summary>
        /// <param name="handler">Handler to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddExceptionHandler(IExceptionHandler handler)
        {
            exceptionHandlers.Add(handler);
            return this;
        }
        /// <summary>
        /// Adds a <see cref="IServerUsageMonitor"/> to the server.
        /// </summary>
        /// <param name="serverUsageMonitor">Monitor to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddServerUsageMonitor(IServerUsageMonitor serverUsageMonitor)
        {
            serverUsageMonitors.Add(serverUsageMonitor);
            return this;
        }
        /// <summary>
        /// Get the resource of provided type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetResource<T>()
        {
            return (T)Resources[typeof(T)];
        }
        /// <summary>
        /// Get handler of provided type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetHandler<T>() where T : class
        {
            foreach(var handler in handlers)
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
            foreach (var exceptionHandler in exceptionHandlers)
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
            foreach (var logger in loggers)
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
            foreach (var serverMonitor in serverUsageMonitors)
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
            foreach (ILogger logger in loggers)
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
            foreach (ILogger logger in loggers)
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
            listener?.Stop();
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            running = true;
            Log("Server started on: " + listener.LocalEndpoint.ToString());
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
                    CheckAndRemoveInactiveClients();
                }
                catch (Exception e)
                {
                    foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
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
                CheckAndGatherMessages();
                /*
                 * Check if the server has any pending connections.
                 * If it has a new connection, process it.
                 */
                try
                {
                    while (listener.Pending())
                    {
                        TcpClient tcpClient = listener.AcceptTcpClient();
                        ClientData clientStruct = new ClientData(tcpClient);
                        Task.Run(() => AcceptClient(clientStruct));
                    }
                }
                catch (Exception e)
                {
                    foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
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
                while(this._ConsumerClientQueue.TryDequeue(out var client)) 
                {
                    Log("Accepted new connection: " + client.TcpClient.Client.RemoteEndPoint.ToString());
                    clients.Add(client);
                    foreach (IHandler handler in handlers)
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

                Scheduler.ScheduleHandling(
                    clients
                        .Where(client => client.ToBeRemoved == false)
                        .Select(client => (client, (client as IQueueHolder<Message>).ConsumerQueue))
                        .ToList(),
                    HandleClientMessages);

                /*
                 * Iterate through all the handlers, running periodic operations.
                 */
                foreach(IHandler handler in handlers)
                {
                    TickHandler(handler);
                }

                /*
                 * Check if there are messages queued to be sent.
                 */
                SendQueuedMessages();

                /*
                 * Call the usage monitors and let them scale or determine current resource usage.
                 */
                foreach (IServerUsageMonitor usageMonitor in serverUsageMonitors)
                {
                    try
                    {
                        usageMonitor.Tick(this);
                    }
                    catch(Exception e)
                    {
                        foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
                        {
                            if (exceptionHandler.HandleException(e))
                            {
                                break;
                            }
                        }
                    }
                }
            }
            listener.Stop();
            foreach (var client in Clients)
            {
                try
                {
                    client?.SslStream?.Dispose();
                    client?.TcpClient?.Dispose();
                }
                catch (Exception e)
                {
                    foreach (var handler in exceptionHandlers)
                    {
                        handler.HandleException(e);
                    }
                }
            }
            listener = null;
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
            while (this._ConsumerMessageOutQueue.TryDequeue(out var tuple))
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
                    for (int i = handlers.Count - 1; i >= 0; i--)
                    {
                        IHandler handler = handlers[i];
                        handler.HandleSendMessage(this, client, ref sendMessage);
                    }
                    CommunicationPrimitives.SendMessage(client.TcpClient, sendMessage, client.SslStream);
                    (client as IActiveClient).UpdateLastActivity();
                    LogDebug("Sent message to " + client.TcpClient.Client.RemoteEndPoint.ToString() +
                        "\nMessage length: " + sendMessage.MessageLength);
                }
                catch(Exception e)
                {
                    foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
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
            foreach (ClientData client in Clients)
            {
                if (!client.TcpClient.Connected || client.ToBeRemoved)
                {
                    toRemove.Add(client);
                }
            }
            foreach (ClientData client in toRemove)
            {
                try
                {
                    foreach (IHandler handler in handlers)
                    {
                        handler.ClientRemoved(this, client);
                    }
                    LogDebug("Client removed: " + client.TcpClient?.Client?.RemoteEndPoint?.ToString());
                    client.Dispose();
                }
                catch(Exception e)
                {
                    foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
                    {
                        if (exceptionHandler.HandleException(e))
                        {
                            break;
                        }
                    }
                }
                clients.Remove(client);
            }
            toRemove.Clear();
        }
        private void TickHandler(IHandler handler)
        {
            try
            {
                handler.Tick(this);
            }
            catch (Exception e)
            {
                foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
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
            foreach(var client in Clients)
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
                    HandleClientMessage(client, message);
                }
                else
                {
                    AffinityHandleClientMessage(client, message);
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
                foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
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
                foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
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
            foreach (IHandler handler in handlers)
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
                    foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
                    {
                        if (exceptionHandler.HandleException(e))
                        {
                            break;
                        }
                    }
                }
            }
            foreach (IHandler handler in handlers)
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
                    foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
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
                        this._ProducerClientQueue.Enqueue(client);
                    }
                    else
                    {
                        client.Dispose();
                    }
                }
                else
                {
                    this._ProducerClientQueue.Enqueue(client);
                }
            }
            catch (Exception e)
            {
                foreach (IExceptionHandler exceptionHandler in exceptionHandlers)
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
