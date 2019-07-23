using MTSC.Common;
using MTSC.Common.Http;
using MTSC.Common.Http.ServerModules;
using System;
using System.Collections.Generic;
using System.Text;

namespace MTSC.Server.Handlers
{
    /// <summary>
    /// Handler for handling server http requests.
    /// </summary>
    public class HttpHandler : IHandler
    {
        #region Fields
        Server managedServer;
        List<IHttpModule> httpModules = new List<IHttpModule>();
        #endregion
        #region Constructors
        public HttpHandler(Server server)
        {
            this.managedServer = server;
        }
        #endregion
        #region Public Methods
        /// <summary>
        /// Add a http module onto the server.
        /// </summary>
        /// <param name="module">Module to be added.</param>
        /// <returns>This handler object.</returns>
        public HttpHandler AddHttpModule(IHttpModule module)
        {
            this.httpModules.Add(module);
            return this;
        }
        /// <summary>
        /// Send a response back to the client.
        /// </summary>
        /// <param name="response">Message containing the response.</param>
        public void SendResponse(ClientStruct client, HttpMessage response)
        {
            byte[] responseBytes = response.GetResponse();
            managedServer.QueueMessage(client, responseBytes);
        }
        #endregion
        #region Interface Implementation
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        /// <param name="client"></param>
        void IHandler.ClientRemoved(ClientStruct client)
        {
            
        }
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        bool IHandler.HandleClient(ClientStruct client)
        {
            return false;
        }
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool IHandler.HandleReceivedMessage(ClientStruct client, Message message)
        {
            HttpMessage httpMessage = new HttpMessage();
            httpMessage.ParseRequest(message.MessageBytes);
            foreach(IHttpModule module in httpModules)
            {
                if(module.HandleRequest(this, client, httpMessage))
                {
                    break;
                }
            }
            return false;
        }
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool IHandler.HandleSendMessage(ClientStruct client, ref Message message)
        {
            return false;
        }
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool IHandler.PreHandleReceivedMessage(ClientStruct client, ref Message message)
        {
            return false;
        }
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        void IHandler.Tick()
        {
            
        }
        #endregion
    }
}
