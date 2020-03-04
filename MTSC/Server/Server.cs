﻿using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.Server.Handlers;
using MTSC.Server.Resources;
using MTSC.Server.UsageMonitors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace MTSC.Server
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
        int port = 80;
        List<ClientData> toRemove = new List<ClientData>();
        List<IHandler> handlers = new List<IHandler>();
        List<ILogger> loggers = new List<ILogger>();
        List<IExceptionHandler> exceptionHandlers = new List<IExceptionHandler>();
        List<IServerUsageMonitor> serverUsageMonitors = new List<IServerUsageMonitor>();
        ConcurrentQueue<Tuple<ClientData, byte[]>> messageQueue = new ConcurrentQueue<Tuple<ClientData, byte[]>>();
        #endregion
        #region Properties
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
        public int Port { get => port; set => port = value; }
        /// <summary>
        /// Returns the state of the server.
        /// </summary>
        public bool Running { get => running; }
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
            this.port = port;
        }
        /// <summary>
        /// Creates an instance of server.
        /// </summary>
        /// <param name="certificate">Certificate for SSL.</param>
        /// <param name="port">Port to be used by the server.</param>
        public Server(X509Certificate2 certificate, int port)
        {
            this.certificate = certificate;
            this.port = port;
        }
        #endregion
        #region Public Methods
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
            this.port = port;
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
        public IResource GetResource<T>()
        {
            return Resources[typeof(T)];
        }
        /// <summary>
        /// Queues a message to be sent.
        /// </summary>
        /// <param name="target">Target client.</param>
        /// <param name="message">Message to be sent.</param>
        public void QueueMessage(ClientData target, byte[] message)
        {
            messageQueue.Enqueue(new Tuple<ClientData, byte[]>(target, message));
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
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            running = true;
            Log("Server started on: " + listener.LocalEndpoint.ToString());
            DateTime lastLoad = DateTime.Now;
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
                        lastLoad = DateTime.Now;
                        TcpClient tcpClient = listener.AcceptTcpClient();
                        ClientData clientStruct = new ClientData(tcpClient);
                        if (this.certificate != null)
                        {
                            try
                            {
                                SslStream sslStream = new SslStream(tcpClient.GetStream(),
                                    true,
                                    this.RemoteCertificateValidationCallback,
                                    this.LocalCertificateSelectionCallback,
                                    this.EncryptionPolicy);
                                clientStruct.SslStream = sslStream;
                                sslStream.AuthenticateAsServer(this.certificate, this.RequestClientCertificate, this.SslProtocols, false);
                            }
                            catch(Exception e)
                            {
                                clientStruct.ToBeRemoved = true;
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
                            if (handler.HandleClient(this, clientStruct))
                            {
                                break;
                            }
                        }
                        Clients.Add(clientStruct);
                        Log("Accepted new connection: " + tcpClient.Client.RemoteEndPoint.ToString());
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
                /*
                 * Process in parallel all clients.
                 */
                Parallel.ForEach(Clients, (client) =>
                {
                    try
                    {
                        /*
                         * If the connection has been lost, mark the client to be removed.
                         * Else, check if there is data to be read.
                         */
                        if (!client.TcpClient.Connected)
                        {
                            client.ToBeRemoved = true;
                        }
                        else if (client.TcpClient.Available > 0)
                        {
                            lastLoad = DateTime.Now;
                            Message message = CommunicationPrimitives.GetMessage(client.TcpClient, client.SslStream);
                            client.LastMessageTime = DateTime.Now;
                            LogDebug("Received message from " + client.TcpClient.Client.RemoteEndPoint.ToString() +
                                    "\nMessage length: " + message.MessageLength);
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
                    }
                    catch(Exception e)
                    {
                        foreach(IExceptionHandler exceptionHandler in exceptionHandlers)
                        {
                            if (exceptionHandler.HandleException(e))
                            {
                                break;
                            }
                        }
                    }
                });
                /*
                 * Iterate through all the handlers, running periodic operations.
                 */
                Parallel.ForEach(handlers, (handler) =>
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
                });
                /*
                 * Check if there are messages queued to be sent.
                 */
                if(messageQueue.Count > 0)
                {
                    lastLoad = DateTime.Now;
                    while (messageQueue.Count > 0)
                    {
                        try
                        {
                            if (messageQueue.TryDequeue(out Tuple<ClientData, byte[]> queuedOrder))
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
                /*
                 * Call the usage monitors and let them scale or determine current resource usage.
                 */
                foreach(IServerUsageMonitor usageMonitor in serverUsageMonitors)
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
