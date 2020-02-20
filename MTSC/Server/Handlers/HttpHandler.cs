using MTSC.Common.Http;
using MTSC.Common.Http.ServerModules;
using MTSC.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        List<ClientData> removeFragmentsList = new List<ClientData>();
        List<IHttpModule> httpModules = new List<IHttpModule>();
        ConcurrentQueue<Tuple<ClientData, HttpResponse>> messageOutQueue = new ConcurrentQueue<Tuple<ClientData, HttpResponse>>();
        ConcurrentDictionary<ClientData, (byte[], DateTime)> fragmentedMessages = new ConcurrentDictionary<ClientData, (byte[], DateTime)>();
        #endregion
        #region Public Properties
        public TimeSpan FragmentsExpirationTime { get; set; } = TimeSpan.FromSeconds(15);
        public double MaximumRequestSize { get; set; } = 15000;
        #endregion
        #region Constructors
        public HttpHandler()
        {
            
        }
        #endregion
        #region Public Methods
        public HttpHandler WithMaximumSize(double size)
        {
            this.MaximumRequestSize = size;
            return this;
        }
        /// <summary>
        /// The amount of time fragments are kept in the buffer before being discarded.
        /// </summary>
        /// <param name="duration">Time until fragments expire.</param>
        /// <returns>This handler object.</returns>
        public HttpHandler WithFragmentsExpirationTime(TimeSpan duration)
        {
            this.FragmentsExpirationTime = duration;
            return this;
        }
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
            messageOutQueue.Enqueue(new Tuple<ClientData, HttpResponse>(client, response));
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
            byte[] messageBytes = null;
            try
            {
                if (fragmentedMessages.ContainsKey(client))
                {
                    byte[] previousBytes = fragmentedMessages[client].Item1;
                    if(previousBytes.Length + message.MessageBytes.Length > MaximumRequestSize)
                    {
                        // Discard the message if it is too big
                        fragmentedMessages.TryRemove(client, out _);
                        return false;
                    }
                    byte[] repackagingBuffer = new byte[previousBytes.Length + message.MessageBytes.Length];
                    Array.Copy(previousBytes, 0, repackagingBuffer, 0, previousBytes.Length);
                    Array.Copy(message.MessageBytes, 0, repackagingBuffer, previousBytes.Length, message.MessageBytes.Length);
                    messageBytes = repackagingBuffer;
                }
                else
                {
                    if(message.MessageBytes.Length > MaximumRequestSize)
                    {
                        // Discard the message if it is too big
                        return false;
                    }
                    messageBytes = message.MessageBytes;
                }
                messageBytes = messageBytes.TrimTrailingNullBytes();
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
                fragmentedMessages[client] = (messageBytes, DateTime.Now);
                server.LogDebug(ex.Message);
                server.LogDebug(ex.StackTrace);
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
            while (messageOutQueue.Count > 0)
            {
                if (messageOutQueue.TryDequeue(out Tuple<ClientData, HttpResponse> tuple))
                {
                    server.QueueMessage(tuple.Item1, tuple.Item2.GetPackedResponse(true));
                }
            }
            foreach (IHttpModule module in httpModules)
            {
                module.Tick(server, this);
            }
            removeFragmentsList.Clear();
            foreach(var kvp in fragmentedMessages)
            {
                if((DateTime.Now - kvp.Value.Item2) > FragmentsExpirationTime)
                {
                    removeFragmentsList.Add(kvp.Key);
                }
            }
            foreach(var key in removeFragmentsList)
            {
                fragmentedMessages.TryRemove(key, out _);
            }
        }
        #endregion
    }
}
