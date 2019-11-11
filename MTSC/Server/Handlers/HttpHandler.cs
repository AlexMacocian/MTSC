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
        private static readonly string urlEncodedHeader = "application/x-www-form-urlencoded";
        private static readonly string multipartHeader = "multipart/form-data";
        #region Fields
        List<IHttpModule> httpModules = new List<IHttpModule>();
        ConcurrentQueue<Tuple<ClientData, HttpResponse>> messageQueue = new ConcurrentQueue<Tuple<ClientData, HttpResponse>>();
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
        public void QueueResponse(ClientData client, HttpResponse response)
        {
            messageQueue.Enqueue(new Tuple<ClientData, HttpResponse>(client, response));
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
            HttpRequest request = HttpRequest.FromBytes(message.MessageBytes);
            HttpResponse response = new HttpResponse();
            if(request.Headers.ContainsHeader(HttpMessage.GeneralHeadersEnum.Connection) && 
                request.Headers[HttpMessage.GeneralHeadersEnum.Connection].ToLower() == "close")
            {
                response.Headers[HttpMessage.GeneralHeadersEnum.Connection] = "close";
                client.ToBeRemoved = true;
            }
            else
            {
                response.Headers[HttpMessage.GeneralHeadersEnum.Connection] = "keep-alive";
            }
            foreach(IHttpModule module in httpModules)
            {
                if(module.HandleRequest(server, this, client, request, ref response))
                {
                    break;
                }
            }
            QueueResponse(client, response);
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
                if (messageQueue.TryDequeue(out Tuple<ClientData, HttpResponse> tuple))
                {
                    server.QueueMessage(tuple.Item1, tuple.Item2.GetPackedResponse(true));
                }
            }
            foreach (IHttpModule module in httpModules)
            {
                module.Tick(server, this);
            }
        }
        #endregion
    }
}
