using MTSC.Common.Http;
using MTSC.Common.Http.ServerModules;
using MTSC.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using static MTSC.Common.Http.HttpMessage;

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
        ConcurrentDictionary<ClientData, MemoryStream> fragmentedMessages = new ConcurrentDictionary<ClientData, MemoryStream>();
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
            // Parse the request. If the message is incomplete, return 100 and queue the message to be parsed later.
            HttpRequest request = null;
            try
            {
                byte[] messageBytes = null;
                if (fragmentedMessages.ContainsKey(client))
                {
                    fragmentedMessages[client].Write(message.MessageBytes, 0, message.MessageBytes.Length);
                    messageBytes = fragmentedMessages[client].ToArray();
                }
                else
                {
                    messageBytes = message.MessageBytes;
                }
                request = HttpRequest.FromBytes(messageBytes);
            }
            catch (Exception ex) when (
                ex is IncompleteHeaderKeyException ||
                ex is IncompleteHeaderValueException ||
                ex is IncompleteHttpVersionException ||
                ex is IncompleteMethodException ||
                ex is IncompleteRequestBodyException ||
                ex is IncompleteRequestQueryException ||
                ex is IncompleteRequestURIException || 
                ex is IncompleteRequestException)
            {
                if (fragmentedMessages.ContainsKey(client))
                {
                    fragmentedMessages[client].Write(message.MessageBytes, 0, message.MessageBytes.Length);
                }
                else
                {
                    fragmentedMessages[client] = new MemoryStream();
                    fragmentedMessages[client].Write(message.MessageBytes, 0, message.MessageBytes.Length);
                }
                Return100Continue(server, client);
                return true;
            }
            catch (Exception e)
            {
                throw e;
            }

            // The message has been parsed. If there was a cache for the current message, remove it.
            if (fragmentedMessages.ContainsKey(client))
            {
                fragmentedMessages.TryRemove(client, out _);
            }

            HttpResponse response = new HttpResponse();
            if(request.Headers.ContainsHeader(HttpMessage.GeneralHeaders.Connection) && 
                request.Headers[HttpMessage.GeneralHeaders.Connection].ToLower() == "close")
            {
                response.Headers[HttpMessage.GeneralHeaders.Connection] = "close";
                client.ToBeRemoved = true;
            }
            else
            {
                response.Headers[HttpMessage.GeneralHeaders.Connection] = "keep-alive";
            }
            foreach(IHttpModule module in httpModules)
            {
                if(module.HandleRequest(server, this, client, request, ref response))
                {
                    break;
                }
            }
            QueueResponse(client, response);
            return true;
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
        private void Return100Continue(Server server, ClientData clientData)
        {
            HttpResponse httpResponse = new HttpResponse();
            httpResponse.StatusCode = StatusCodes.Continue;
            httpResponse.Headers[GeneralHeaders.Connection] = "keep-alive";
            server.QueueMessage(clientData, httpResponse.GetPackedResponse(false));
        }
    }
}
