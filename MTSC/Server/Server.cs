using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.Server.Handlers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
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
        int port = 50;
        List<ClientData> toRemove = new List<ClientData>();
        List<ClientData> clients = new List<ClientData>();
        List<IHandler> handlers = new List<IHandler>();
        List<ILogger> loggers = new List<ILogger>();
        List<IExceptionHandler> exceptionHandlers = new List<IExceptionHandler>();
        Queue<Tuple<ClientData, byte[]>> messageQueue = new Queue<Tuple<ClientData, byte[]>>();
        #endregion
        #region Properties
        /// <summary>
        /// Server port.
        /// </summary>
        public int Port { get => port; set => port = value; }
        /// <summary>
        /// Returns the state of the server.
        /// </summary>
        public bool Running { get => running; }
        /// <summary>
        /// List of clients currently connected to the server.
        /// </summary>
        public List<ClientData> Clients { get => clients; set => clients = value; }
        /// <summary>
        /// If set to true, the server will use an algorithm to lower or increase the CPU usage
        /// based on demands.
        /// </summary>
        public bool ScaleUsage { get; set; }
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
        /// Adds a handler to the server.
        /// </summary>
        /// <param name="handler">Handler to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddHandler(IHandler handler)
        {
            handlers.Add(handler);
            return this;
        }
        /// <summary>
        /// Adds a logger to the server.
        /// </summary>
        /// <param name="logger">Logger to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddLogger(ILogger logger)
        {
            loggers.Add(logger);
            return this;
        }
        /// <summary>
        /// Adds an exception handler to the server.
        /// </summary>
        /// <param name="handler">Handler to be added.</param>
        /// <returns>This server object.</returns>
        public Server AddExceptionHandler(IExceptionHandler handler)
        {
            exceptionHandlers.Add(handler);
            return this;
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
        /// <param name="">Message to be logged</param>
        public void Log(string log)
        {
            foreach (ILogger logger in loggers)
            {
                logger.Log(log);
            }
        }
        /// <summary>
        /// Adds a debug message to be logged by the associated loggers.
        /// </summary>
        /// <param name="">Debug message to be logged</param>
        public void LogDebug(string debugMessage)
        {
            foreach (ILogger logger in loggers)
            {
                logger.LogDebug(debugMessage);
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
            while (running)
            {
                /*
                 * Check the client states. If a client is disconnected, 
                 * remove it from the list of clients.
                 */
                try
                {
                    foreach (ClientData client in clients)
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
                        clients.Remove(client);
                    }
                    toRemove.Clear();
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
                /*
                 * Check if the server has any pending connections.
                 * If it has a new connection, process it.
                 */
                try
                {
                    if (listener.Pending())
                    {
                        TcpClient tcpClient = listener.AcceptTcpClient();
                        ClientData clientStruct = new ClientData(tcpClient);
                        if (certificate != null)
                        {
                            SslStream sslStream = new SslStream(tcpClient.GetStream(), true, new RemoteCertificateValidationCallback((o, c, ch, po) => {
                                return true;
                            }), null, EncryptionPolicy.RequireEncryption);
                            clientStruct.SslStream = sslStream;
                            sslStream.AuthenticateAsServer(certificate);
                        }
                        foreach (IHandler handler in handlers)
                        {
                            if (handler.HandleClient(this, clientStruct))
                            {
                                break;
                            }
                        }
                        clients.Add(clientStruct);
                        Log("Accepted new connection: " + tcpClient.Client.RemoteEndPoint.ToString());
                    }
                }
                catch(Exception e)
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
                /*
                 * Process in parallel all clients.
                 */
                Parallel.ForEach(clients, (client) =>
                {
                    try
                    {
                        if (client.TcpClient.Available > 0)
                        {
                            Message message = CommunicationPrimitives.GetMessage(client.TcpClient, client.SslStream);
                            LogDebug("Received message from " + client.TcpClient.Client.RemoteEndPoint.ToString() +
                                    "\nMessage length: " + message.MessageLength);
                            foreach (IHandler handler in handlers)
                            {
                                if (handler.PreHandleReceivedMessage(this, client, ref message))
                                {
                                    break;
                                }
                            }
                            foreach (IHandler handler in handlers)
                            {
                                if (handler.HandleReceivedMessage(this, client, message))
                                {
                                    break;
                                }
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        LogDebug("Exception: " + e.Message);
                        LogDebug("Stacktrace: " + e.StackTrace);
                        foreach(IExceptionHandler exceptionHandler in exceptionHandlers)
                        {
                            if (exceptionHandler.HandleException(e))
                            {
                                break;
                            }
                        }
                    }
                });
                foreach(IHandler handler in handlers)
                {
                    try
                    {
                        handler.Tick(this);
                    }
                    catch(Exception e)
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
                /*
                 * Check if there are messages queued to be sent.
                 */
                if (messageQueue.Count == 0)
                {
                    Thread.Sleep(33);
                }
                else
                {
                    try
                    {
                        while (messageQueue.Count > 0)
                        {
                            Tuple<ClientData, byte[]> queuedOrder = messageQueue.Dequeue();
                            Message sendMessage = CommunicationPrimitives.BuildMessage(queuedOrder.Item2);
                            for (int i = handlers.Count - 1; i >= 0; i--)
                            {
                                IHandler handler = handlers[i];
                                ClientData client = queuedOrder.Item1;
                                handler.HandleSendMessage(this, client, ref sendMessage);
                            }
                            CommunicationPrimitives.SendMessage(queuedOrder.Item1.TcpClient, sendMessage, queuedOrder.Item1.SslStream);
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
        /// <summary>
        /// Runs the server async.
        /// </summary>
        public Task RunAsync()
        {
            return new Task(Run);
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
