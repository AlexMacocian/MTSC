using MTSC.Exceptions;
using MTSC.Logging;
using MTSC.Server.Handlers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        TcpListener listener;
        int port = 50;
        List<ClientStruct> toRemove = new List<ClientStruct>();
        List<ClientStruct> clients = new List<ClientStruct>();
        List<IHandler> handlers = new List<IHandler>();
        List<ILogger> loggers = new List<ILogger>();
        List<IExceptionHandler> exceptionHandlers = new List<IExceptionHandler>();
        Queue<Tuple<ClientStruct, byte[]>> messageQueue = new Queue<Tuple<ClientStruct, byte[]>>();
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
        public List<ClientStruct> Clients { get => clients; set => clients = value; }
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
        public void QueueMessage(ClientStruct target, byte[] message)
        {
            messageQueue.Enqueue(new Tuple<ClientStruct, byte[]>(target, message));
        }
        /// <summary>
        /// Adds a message to be logged by the associated loggers.
        /// </summary>
        /// <param name="">Message to be logged</param>
        public void Log(string log)
        {
            foreach (ILogger logger in loggers)
            {
                logger.Log(log + "\n");
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
                 * Check if there are messages queued to be sent.
                 */
                if(messageQueue.Count > 0)
                {
                    Tuple<ClientStruct, byte[]> queuedOrder = messageQueue.Dequeue();
                    Message sendMessage = CommunicationPrimitives.BuildMessage(queuedOrder.Item2);
                    foreach(IHandler handler in handlers)
                    {
                        handler.HandleSendMessage(queuedOrder.Item1, ref sendMessage);
                    }
                    CommunicationPrimitives.SendMessage(queuedOrder.Item1.TcpClient, sendMessage);
                }

                /*
                 * Check if the server has any pending connections.
                 * If it has a new connection, process it.
                 */ 
                if (listener.Pending())
                {
                    TcpClient tcpClient = listener.AcceptTcpClient();
                    ClientStruct clientStruct = new ClientStruct(tcpClient);
                    foreach(IHandler handler in handlers)
                    {
                        if (handler.HandleClient(clientStruct))
                        {
                            break;
                        }
                    }
                    clients.Add(clientStruct);
                    Log("Accepted new connection: " + tcpClient.Client.RemoteEndPoint.ToString());
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
                            Message message = CommunicationPrimitives.GetMessage(client.TcpClient);
                            Log("Received message from " + client.TcpClient.Client.RemoteEndPoint.ToString() +
                                    " Message length: " + message.MessageLength);
                            foreach (IHandler handler in handlers)
                            {
                                if (handler.PreHandleReceivedMessage(client, ref message))
                                {
                                    break;
                                }
                            }
                            foreach (IHandler handler in handlers)
                            {
                                if (handler.HandleReceivedMessage(client, message))
                                {
                                    break;
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
                 * Check the client states. If a client is disconnected, 
                 * remove it from the list of clients.
                 */
                foreach(ClientStruct client in clients)
                {
                    if (!client.TcpClient.Connected || client.ToBeRemoved)
                    {
                        toRemove.Add(client);
                    }
                }
                foreach(ClientStruct client in toRemove)
                {
                    foreach(IHandler handler in handlers)
                    {
                        handler.ClientRemoved(client);
                    }
                    client.TcpClient?.Dispose();
                    clients.Remove(client);
                }
                toRemove.Clear();
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
    public struct ClientStruct
    {
        public TcpClient TcpClient;
        public DateTime LastMessageTime;
        public bool ToBeRemoved;

        public ClientStruct(TcpClient client)
        {
            this.TcpClient = client;
            this.LastMessageTime = DateTime.Now;
            ToBeRemoved = false;
        }
    }
}
