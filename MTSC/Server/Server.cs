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
        List<TcpClient> toRemove = new List<TcpClient>();
        List<TcpClient> clients = new List<TcpClient>();
        List<IHandler> handlers = new List<IHandler>();
        List<ILogger> loggers = new List<ILogger>();
        List<IExceptionHandler> exceptionHandlers = new List<IExceptionHandler>();
        #endregion
        #region Properties
        public int Port { get => port; set => port = value; }
        /// <summary>
        /// Returns the state of the server.
        /// </summary>
        public bool Running { get => running; }
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
        /// Blocking method. Runs the server on the current thread.
        /// </summary>
        public void Run()
        {
            listener?.Stop();
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            while (running)
            {
                /*
                 * Check if the server has any pending connections.
                 * If it has a new connection, process it.
                 */ 
                if (listener.Pending())
                {
                    TcpClient tcpClient = listener.AcceptTcpClient();
                    foreach(IHandler handler in handlers)
                    {
                        if (handler.HandleClient(tcpClient))
                        {
                            break;
                        }
                    }
                    clients.Add(tcpClient);
                }
                /*
                 * Process in parallel all clients.
                 */
                Parallel.ForEach(clients, (client) =>
                {
                    try
                    {
                        if (client.Available > 0)
                        {
                            Message message = CommunicationPrimitives.GetMessage(client);
                            foreach (ILogger logger in loggers)
                            {
                                logger.Log("Received message from " + client.Client.RemoteEndPoint.ToString() + 
                                    " Message length: " + message.MessageLength);
                            }
                            foreach(IHandler handler in handlers)
                            {
                                if (handler.HandleMessage(out message))
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
                foreach(TcpClient client in clients)
                {
                    if (!client.Connected)
                    {
                        toRemove.Add(client);
                    }
                }
                foreach(TcpClient client in toRemove)
                {
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
}
