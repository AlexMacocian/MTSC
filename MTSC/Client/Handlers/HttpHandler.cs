using MTSC.Common.Http;
using MTSC.Common.Http.ClientModules;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MTSC.Client.Handlers
{
    /// <summary>
    /// Handler for handling client http communication.
    /// </summary>
    public class HttpHandler : IHandler
    {
        #region Fields
        Client managedClient;
        List<IHttpModule> httpModules = new List<IHttpModule>();
        #endregion
        #region Constructors
        public HttpHandler(Client client)
        {
            managedClient = client;
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Send a request to the server.
        /// </summary>
        /// <param name="request">Request to be sent.</param>
        public void SendRequest(HttpMessage request)
        {
            managedClient.QueueMessage(request.GetRequest());
        }
        /// <summary>
        /// Add a http module.
        /// </summary>
        /// <param name="httpModule">Module to be added.</param>
        /// <returns>This handler object.</returns>
        public HttpHandler AddModule(IHttpModule httpModule)
        {
            httpModules.Add(httpModule);
            return this;
        }
        #endregion
        #region Interface Implementation
        /// <summary>
        /// Handler implementation
        /// </summary>
        /// <param name="client"></param>
        void IHandler.Disconnected(TcpClient client)
        {
            
        }
        /// <summary>
        /// Handler implementation
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns>False.</returns>
        bool IHandler.HandleReceivedMessage(TcpClient client, Message message)
        {
            HttpMessage httpMessage = new HttpMessage();
            httpMessage.ParseResponse(message.MessageBytes);
            foreach(IHttpModule httpModule in httpModules)
            {
                if (httpModule.HandleResponse(this, httpMessage))
                {
                    break;
                }
            }
            return false;
        }
        /// <summary>
        /// Handler implementation
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns>False.</returns>
        bool IHandler.HandleSendMessage(TcpClient client, ref Message message)
        {
            return false;
        }
        /// <summary>
        /// Handler implementation.
        /// </summary>
        /// <param name="client"></param>
        /// <returns>True.</returns>
        bool IHandler.InitializeConnection(TcpClient client)
        {
            return true;
        }
        /// <summary>
        /// Handler imeplementation.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns>False.</returns>
        bool IHandler.PreHandleReceivedMessage(TcpClient client, ref Message message)
        {
            return false;
        }
        /// <summary>
        /// Handler implementation.
        /// </summary>
        /// <param name="tcpClient"></param>
        void IHandler.Tick(TcpClient tcpClient)
        {
            
        }
        #endregion
    }
}
