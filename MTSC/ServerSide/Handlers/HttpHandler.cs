using MTSC.Common.Http;
using MTSC.Common.Http.ServerModules;
using MTSC.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.ServerSide.Handlers
{
    /// <summary>
    /// Handler for handling server http requests.
    /// </summary>
    public sealed class HttpHandler : IHandler
    {
        private static readonly string urlEncodedHeader = "application/x-www-form-urlencoded";
        private static readonly string multipartHeader = "multipart/form-data";
        #region Fields
        List<IHttpModule> httpModules = new List<IHttpModule>();
        ConcurrentQueue<Tuple<ClientData, HttpResponse>> messageOutQueue = new ConcurrentQueue<Tuple<ClientData, HttpResponse>>();
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
                var trimmedMessageBytes = message.MessageBytes.TrimTrailingNullBytes();
                if (client.Resources.TryGetResource<FragmentedMessage>(out var fragmentedMessage))
                {
                    byte[] previousBytes = fragmentedMessage.Message;
                    if (previousBytes.Length + trimmedMessageBytes.Length > MaximumRequestSize)
                    {
                        // Discard the message if it is too big
                        server.LogDebug($"Discarded message. Message size [{previousBytes.Length + trimmedMessageBytes.Length}] > [{MaximumRequestSize}]");
                        client.Resources.RemoveResource<FragmentedMessage>();
                        QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request disallowed because it exceeds [{MaximumRequestSize}] bytes!" });
                        return false;
                    }
                    byte[] repackagingBuffer = new byte[previousBytes.Length + trimmedMessageBytes.Length];
                    Array.Copy(previousBytes, 0, repackagingBuffer, 0, previousBytes.Length);
                    Array.Copy(trimmedMessageBytes, 0, repackagingBuffer, previousBytes.Length, trimmedMessageBytes.Length);
                    messageBytes = repackagingBuffer;
                }
                else
                {
                    if (trimmedMessageBytes.Length > MaximumRequestSize)
                    {
                        // Discard the message if it is too big
                        server.LogDebug($"Discarded message. Message size [{message.MessageBytes.Length}] > [{MaximumRequestSize}]");
                        QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request disallowed because it exceeds [{MaximumRequestSize}] bytes!" });
                        return false;
                    }
                    messageBytes = trimmedMessageBytes;
                }
                messageBytes = messageBytes.TrimTrailingNullBytes();
                var partialRequest = PartialHttpRequest.FromBytes(messageBytes);
                if (partialRequest.Complete)
                    request = partialRequest.ToRequest();
                else
                {
                    HandleIncompleteRequest(client, server, messageBytes, partialRequest);
                    return false;
                }
            }
            catch (Exception ex) when (
                ex is IncompleteHeaderKeyException ||
                ex is IncompleteHeaderValueException ||
                ex is IncompleteHttpVersionException ||
                ex is IncompleteMethodException ||
                ex is IncompleteRequestBodyException ||
                ex is IncompleteRequestQueryException ||
                ex is IncompleteRequestURIException ||
                ex is IncompleteRequestException ||
                ex is InvalidPostFormException)
            {
                server.LogDebug(ex.Message);
                server.LogDebug(ex.StackTrace);
                HandleIncompleteRequest(client, server, messageBytes);
                return false;
            }
            catch (Exception e)
            {
                throw e;
            }

            // The message has been parsed. If there was a cache for the current message, remove it.
            if (client.Resources.Contains<FragmentedMessage>())
            {
                client.Resources.RemoveResource<FragmentedMessage>();
            }

            HttpResponse response = new HttpResponse();
            if (request.Headers.ContainsHeader(HttpMessage.GeneralHeaders.Connection) &&
                request.Headers[HttpMessage.GeneralHeaders.Connection].ToLower() == "close")
            {
                response.Headers[HttpMessage.GeneralHeaders.Connection] = "close";
                client.ToBeRemoved = true;
            }
            else
            {
                response.Headers[HttpMessage.GeneralHeaders.Connection] = "keep-alive";
            }
            foreach (IHttpModule module in httpModules)
            {
                if (module.HandleRequest(server, this, client, request, ref response))
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
            foreach (var client in server.Clients)
            {
                if (client.Resources.TryGetResource<FragmentedMessage>(out var fragmentedMessage))
                {
                    if ((DateTime.Now - fragmentedMessage.LastReceived) > FragmentsExpirationTime)
                    {
                        QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request timed out in [{FragmentsExpirationTime.TotalMilliseconds}] ms!" });
                        client.Resources.RemoveResource<FragmentedMessage>();
                    }
                }
            }
        }
        #endregion

        private void HandleIncompleteRequest(ClientData client, Server server, byte[] messageBytes, PartialHttpRequest partialRequest = null)
        {
            client.Resources.SetResource(new FragmentedMessage() { Message = messageBytes, LastReceived = DateTime.Now });
            server.LogDebug("Incomplete request received!");
            if (partialRequest != null && partialRequest.Headers.ContainsHeader(HttpMessage.RequestHeaders.Expect) &&
                partialRequest.Headers[HttpMessage.RequestHeaders.Expect].Equals("100-continue", StringComparison.OrdinalIgnoreCase))
            {
                server.LogDebug("Returning 100-Continue");
                var contResponse = new HttpResponse { StatusCode = HttpMessage.StatusCodes.Continue };
                contResponse.Headers[HttpMessage.GeneralHeaders.Connection] = "keep-alive";
                QueueResponse(client, contResponse);
            }
        }
        private class FragmentedMessage
        {
            public byte[] Message { get; set; }

            public DateTime LastReceived { get; set; } = DateTime.Now;

            public void AddToMessage(byte[] bytes)
            {
                byte[] newMessage = new byte[Message.Length + bytes.Length];
                Array.Copy(Message, newMessage, Message.Length);
                Array.Copy(bytes, 0, newMessage, Message.Length, bytes.Length);
            }
        }
    }
}
