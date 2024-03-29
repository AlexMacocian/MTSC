﻿using MTSC.Common.Http;
using MTSC.Common.Http.ClientModules;
using System.Collections.Generic;

namespace MTSC.Client.Handlers
{
    /// <summary>
    /// Handler for handling client http communication.
    /// </summary>
    public sealed class HttpHandler : IHandler
    {
        #region Fields
        List<IHttpModule> httpModules = new();
        #endregion
        #region Constructors
        public HttpHandler()
        {

        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Send a request to the server.
        /// </summary>
        /// <param name="request">Request to be sent.</param>
        public void SendRequest(Client client, HttpRequest request)
        {
            client.QueueMessage(request.GetPackedRequest());
        }
        /// <summary>
        /// Add a http module.
        /// </summary>
        /// <param name="httpModule">Module to be added.</param>
        /// <returns>This handler object.</returns>
        public HttpHandler AddModule(IHttpModule httpModule)
        {
            this.httpModules.Add(httpModule);
            return this;
        }
        #endregion
        #region Interface Implementation
        /// <summary>
        /// Handler implementation
        /// </summary>
        /// <param name="client"></param>
        void IHandler.Disconnected(Client client)
        {
            
        }
        /// <summary>
        /// Handler implementation
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns>False.</returns>
        bool IHandler.HandleReceivedMessage(Client client, Message message)
        {
            var httpMessage = new HttpResponse(message.MessageBytes);
            foreach(var httpModule in this.httpModules)
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
        bool IHandler.HandleSendMessage(Client client, ref Message message)
        {
            return false;
        }
        /// <summary>
        /// Handler implementation.
        /// </summary>
        /// <param name="client"></param>
        /// <returns>True.</returns>
        bool IHandler.InitializeConnection(Client client)
        {
            return true;
        }
        /// <summary>
        /// Handler imeplementation.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns>False.</returns>
        bool IHandler.PreHandleReceivedMessage(Client client, ref Message message)
        {
            return false;
        }
        /// <summary>
        /// Handler implementation.
        /// </summary>
        /// <param name="tcpClient"></param>
        void IHandler.Tick(Client tcpClient)
        {
            
        }
        #endregion
    }
}
