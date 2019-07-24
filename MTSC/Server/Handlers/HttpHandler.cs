using MTSC.Common;
using MTSC.Common.Http;
using MTSC.Common.Http.ServerModules;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MTSC.Server.Handlers
{
    /// <summary>
    /// Handler for handling server http requests.
    /// </summary>
    public class HttpHandler : IHandler
    {
        #region Fields
        List<IHttpModule> httpModules = new List<IHttpModule>();
        ConcurrentQueue<Tuple<ClientData,HttpMessage>> messageQueue = new ConcurrentQueue<Tuple<ClientData, HttpMessage>>();
        object queueLock = new object();
        #endregion
        #region Constructors
        public HttpHandler()
        {
            
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
        public void QueueResponse(ClientData client, HttpMessage response)
        {
            messageQueue.Enqueue(new Tuple<ClientData, HttpMessage>(client, response));
        }
        #endregion
        #region Interface Implementation
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        /// <param name="client"></param>
        void IHandler.ClientRemoved(Server server, ClientData client)
        {
            
        }
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        bool IHandler.HandleClient(Server server, ClientData client)
        {
            return false;
        }
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool IHandler.HandleReceivedMessage(Server server, ClientData client, Message message)
        {
            HttpMessage httpMessage = new HttpMessage();
            HttpMessage responseMessage = new HttpMessage();
            httpMessage.ParseRequest(message.MessageBytes);
            if(httpMessage.ContainsHeader(HttpMessage.GeneralHeadersEnum.Connection) && 
                httpMessage[HttpMessage.GeneralHeadersEnum.Connection].ToLower() == "close")
            {
                responseMessage[HttpMessage.GeneralHeadersEnum.Connection] = "close";
                client.ToBeRemoved = true;
            }
            else
            {
                responseMessage[HttpMessage.GeneralHeadersEnum.Connection] = "keep-alive";
            }
            foreach(IHttpModule module in httpModules)
            {
                if(module.HandleRequest(this, client, httpMessage, ref responseMessage))
                {
                    break;
                }
            }
            QueueResponse(client, responseMessage);
            return false;
        }
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool IHandler.HandleSendMessage(Server server, ClientData client, ref Message message)
        {
            return false;
        }
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        bool IHandler.PreHandleReceivedMessage(Server server, ClientData client, ref Message message)
        {
            return false;
        }
        /// <summary>
        /// Handler interface implementation.
        /// </summary>
        void IHandler.Tick(Server server)
        {
            while (messageQueue.Count > 0)
            {
                Tuple<ClientData, HttpMessage> tuple = null;
                if (messageQueue.TryDequeue(out tuple))
                {
                    server.QueueMessage(tuple.Item1, tuple.Item2.GetResponse(true));
                }
            }
        }
        #endregion
    }
}
