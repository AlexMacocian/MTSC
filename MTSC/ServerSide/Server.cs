using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.ServerSide.Handlers;
using MTSC.ServerSide.Resources;
using MTSC.ServerSide.Schedulers;
using MTSC.ServerSide.UsageMonitors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        List<ClientData> toRemove = new List<ClientData>();
        List<IHandler> handlers = new List<IHandler>();
        List<ILogger> loggers = new List<ILogger>();
        List<IExceptionHandler> exceptionHandlers = new List<IExceptionHandler>();
        List<IServerUsageMonitor> serverUsageMonitors = new List<IServerUsageMonitor>();
        #endregion
        #region Properties
        public IScheduler Scheduler { get; set; } = new ParallelScheduler();
        public IProducerConsumerCollection<(ClientData, Message)> InQueue { get; set; } = new ConcurrentQueue<(ClientData, Message)>();
        public IProducerConsumerCollection<(ClientData, byte[])> OutQueue { get; set; } = new ConcurrentQueue<(ClientData, byte[])>();
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
        public List<ClientData> Clients { get; set; } = new List<ClientData>();
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
        /// Sets the InQueue with the provided type.
        /// Default type is a <see cref="ConcurrentQueue{T}"/>.
        /// Modifying the queue will have an impact on performance, depending on the implementation of the provided collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <returns>This server object.</returns>
        public Server SetInQueue<T>(T queue) where T : IProducerConsumerCollection<(ClientData, Message)>
        {
            this.InQueue = queue;
            return this;
        }
        /// <summary>
        /// Sets the OutQueue with the provided type.
        /// Default type is a <see cref="ConcurrentQueue{T}"/>.
        /// Modifying the queue will have an impact on performance, depending on the implementation of the provided collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <returns>This server object.</returns>
        public Server SetOutQueue<T>(T queue) where T : IProducerConsumerCollection<(ClientData, byte[])>
        {
            this.OutQueue = queue;
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
            int retries = 0;
            while(!OutQueue.TryAdd((target, message))) 
            {
                retries++;
                if(retries > 5)
                {
                    throw new QueueOperationException($"Failed to insert provided message in the {nameof(OutQueue)}. Tried {retries} times");
                }
            };
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
                 * Check if the server has any pending connections.
                 * If it has a new connection, process it.
                 */
                try
                {
                    while (listener.Pending())
                    {
                        TcpClient tcpClient = listener.AcceptTcpClient();
                        ClientData clientStruct = new ClientData(tcpClient);
                        Clients.Add(clientStruct);
                        foreach (IHandler handler in handlers)
                        {
                            if (handler.HandleClient(this, clientStruct))
                            {
                                break;
                            }
                        }
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
                 * Gather all messages from clients and put them in a queue
                 */
                GatherReceivedMessages();

                /*
                 * Call the scheduler to handle all received messages and distribute them to the handlers
                 */

                Scheduler.ScheduleHandling(InQueue, HandleClientMessage);

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
            while (OutQueue.Count > 0)
            {
                try
                {
                    if (OutQueue.TryTake(out (ClientData, byte[]) queuedOrder))
                    {
                        Message sendMessage = CommunicationPrimitives.BuildMessage(queuedOrder.Item2);
                        for (int i = handlers.Count - 1; i >= 0; i--)
                        {
                            IHandler handler = handlers[i];
                            ClientData client = queuedOrder.Item1;
                            handler.HandleSendMessage(this, client, ref sendMessage);
                        }
                        CommunicationPrimitives.SendMessage(queuedOrder.Item1.TcpClient, sendMessage, queuedOrder.Item1.SslStream);
                        LogDebug("Sent message to " + queuedOrder.Item1.TcpClient.Client.RemoteEndPoint.ToString() +
                            "\nMessage length: " + sendMessage.MessageLength);
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
                foreach (IHandler handler in handlers)
                {
                    handler.ClientRemoved(this, client);
                }
                LogDebug("Client removed: " + client.TcpClient.Client.RemoteEndPoint.ToString());
                client.SslStream?.Dispose();
                client.TcpClient?.Dispose();
                Clients.Remove(client);
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

        private void GatherReceivedMessages()
        {
            foreach(var client in Clients)
            {
                if (!client.TcpClient.Connected)
                {
                    client.ToBeRemoved = true;
                }
                try
                {
                    if (!client.ToBeRemoved && client.TcpClient.Available > 0)
                    {
                        Message message = CommunicationPrimitives.GetMessage(client.TcpClient, client.SslStream);
                        client.LastMessageTime = DateTime.Now;
                        LogDebug("Received message from " + client.TcpClient.Client.RemoteEndPoint.ToString() +
                                "\nMessage length: " + message.MessageLength);
                        int retries = 0;
                        while(!InQueue.TryAdd((client, message)))
                        {
                            retries++;
                            if(retries > 5)
                            {
                                throw new QueueOperationException($"Failed to insert received message in {nameof(InQueue)}. Tried [{retries}] times");
                            }
                        }
                    }
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
                    LogDebug("Exception: " + e.Message);
                    LogDebug("Stacktrace: " + e.StackTrace);
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

            Log("Accepted new connection: " + client.TcpClient.Client.RemoteEndPoint.ToString());
            if (this.certificate != null)
            {
                try
                {
                    SslStream sslStream = new SslStream(client.TcpClient.GetStream(),
                        true,
                        this.RemoteCertificateValidationCallback,
                        this.LocalCertificateSelectionCallback,
                        this.EncryptionPolicy);
                    client.SslStream = sslStream;
                    if (!sslStream.AuthenticateAsServerAsync(this.certificate, this.RequestClientCertificate, this.SslProtocols, false).Wait(SslAuthenticationTimeout))
                    {
                        client.ToBeRemoved = true;
                    }
                }
                catch (Exception e)
                {
                    client.ToBeRemoved = true;
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
        #endregion
    }
    /// <summary>
    /// Structure containing client information.
    /// </summary>
    public class ClientData
    {
        public TcpClient TcpClient;
        public DateTime LastMessageTime;
        public bool ToBeRemoved;
        public SslStream SslStream;

        public ClientData(TcpClient client)
        {
            this.TcpClient = client;
            this.LastMessageTime = DateTime.Now;
            ToBeRemoved = false;
            SslStream = null;
        }
    }
}
