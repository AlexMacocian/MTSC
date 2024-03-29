﻿using MTSC.Common.Http;
using MTSC.Common.Http.ServerModules;
using MTSC.Common.Http.Telemetry;
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
        #region Fields
        private readonly List<IHttpLogger> httpLoggers = new();
        private readonly List<IHttpModule> httpModules = new();
        private readonly ConcurrentQueue<Tuple<ClientData, HttpResponse>> messageOutQueue = new();
        #endregion
        #region Public Properties
        public bool Return500OnException { get; set; } = true;
        public TimeSpan FragmentsExpirationTime { get; set; } = TimeSpan.FromSeconds(15);
        public double MaximumRequestSize { get; set; } = 15000;
        #endregion
        #region Constructors
        public HttpHandler()
        {

        }
        #endregion
        #region Public Methods
        public HttpHandler AddHttpLogger(IHttpLogger httpLogger) 
        {
            this.httpLoggers.Add(httpLogger);
            return this;
        }
        public HttpHandler WithMaximumSize(double size)
        {
            this.MaximumRequestSize = size;
            return this;
        }
        public HttpHandler WithReturn500OnException(bool return500OnException)
        {
            this.Return500OnException = return500OnException;
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
            this.messageOutQueue.Enqueue(new Tuple<ClientData, HttpResponse>(client, response));
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
            HttpRequest request;
            try
            {
                var trimmedMessageBytes = message.MessageBytes.TrimTrailingNullBytes();
                byte[] messageBytes;
                if (client.Resources.TryGetResource<FragmentedMessage>(out var fragmentedMessage))
                {
                    var previousBytes = fragmentedMessage.Message;
                    if (previousBytes.Length + trimmedMessageBytes.Length > this.MaximumRequestSize)
                    {
                        // Discard the message if it is too big
                        server.LogDebug($"Discarded message. Message size [{previousBytes.Length + trimmedMessageBytes.Length}] > [{this.MaximumRequestSize}]");
                        client.Resources.RemoveResource<FragmentedMessage>();
                        this.QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request disallowed because it exceeds [{this.MaximumRequestSize}] bytes!" });
                        return true;
                    }

                    var repackagingBuffer = new byte[previousBytes.Length + trimmedMessageBytes.Length];
                    Array.Copy(previousBytes, 0, repackagingBuffer, 0, previousBytes.Length);
                    Array.Copy(trimmedMessageBytes, 0, repackagingBuffer, previousBytes.Length, trimmedMessageBytes.Length);
                    messageBytes = repackagingBuffer;
                }
                else
                {
                    if (trimmedMessageBytes.Length > this.MaximumRequestSize)
                    {
                        // Discard the message if it is too big
                        server.LogDebug($"Discarded message. Message size [{message.MessageBytes.Length}] > [{this.MaximumRequestSize}]");
                        this.QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request disallowed because it exceeds [{this.MaximumRequestSize}] bytes!" });
                        return true;
                    }

                    messageBytes = trimmedMessageBytes;
                }

                messageBytes = messageBytes.TrimTrailingNullBytes();
                var partialRequest = PartialHttpRequest.FromBytes(messageBytes);
                if (partialRequest.Complete)
                {
                    request = partialRequest.ToRequest();
                    client.ResetAffinityIfMe(this);
                }
                else
                {
                    client.SetAffinity(this);
                    this.HandleIncompleteRequest(client, server, messageBytes);
                    if (partialRequest != null && partialRequest.Headers.ContainsHeader(RequestHeaders.Expect) &&
                        partialRequest.Headers[RequestHeaders.Expect].Equals("100-continue", StringComparison.OrdinalIgnoreCase))
                    {
                        server.LogDebug("Returning 100-Continue");
                        var contResponse = new HttpResponse { StatusCode = HttpMessage.StatusCodes.Continue };
                        contResponse.Headers[HttpMessage.GeneralHeaders.Connection] = "keep-alive";
                        this.QueueResponse(client, contResponse);
                    }

                    return true;
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
                server.LogDebug("Malformed request, not saving!");
                server.LogDebug(ex.Message + "\n" + ex.StackTrace);
                client.ResetAffinityIfMe(this);
                return false;
            }
            catch (Exception)
            {
                client.ResetAffinityIfMe(this);
                throw;
            }

            // The message has been parsed. If there was a cache for the current message, remove it.
            if (client.Resources.Contains<FragmentedMessage>())
            {
                client.Resources.RemoveResource<FragmentedMessage>();
            }

            HttpResponse response = new();
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

            foreach (var httpLogger in this.httpLoggers)
            {
                httpLogger.LogRequest(server, this, client, request);
            }

            foreach (var module in this.httpModules)
            {
                try
                {
                    if (module.HandleRequest(server, this, client, request, ref response))
                    {
                        break;
                    }
                }
                catch
                {
                    if (this.Return500OnException)
                    {
                        response.StatusCode = StatusCodes.InternalServerError;
                        response.BodyString = "An exception ocurred while processing the request";
                        break;
                    }

                    throw;
                }
            }

            foreach (var httpLogger in this.httpLoggers)
            {
                httpLogger.LogResponse(server, this, client, response);
            }

            this.QueueResponse(client, response);
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
            while (this.messageOutQueue.Count > 0)
            {
                if (this.messageOutQueue.TryDequeue(out var tuple))
                {
                    server.QueueMessage(tuple.Item1, tuple.Item2.GetPackedResponse(true));
                }
            }

            foreach (var module in this.httpModules)
            {
                module.Tick(server, this);
            }

            foreach (var client in server.Clients)
            {
                if (client.Resources.TryGetResource<FragmentedMessage>(out var fragmentedMessage))
                {
                    if ((DateTime.Now - fragmentedMessage.LastReceived) > this.FragmentsExpirationTime)
                    {
                        this.QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request timed out in [{this.FragmentsExpirationTime.TotalMilliseconds}] ms!" });
                        client.Resources.RemoveResource<FragmentedMessage>();
                    }
                }
            }
        }
        #endregion

        private void HandleIncompleteRequest(ClientData client, Server server, byte[] messageBytes)
        {
            client.Resources.SetResource(new FragmentedMessage() { Message = messageBytes, LastReceived = DateTime.Now });
            server.LogDebug("Incomplete request received!");
        }
        private class FragmentedMessage
        {
            public byte[] Message { get; set; }

            public DateTime LastReceived { get; set; } = DateTime.Now;

            public void AddToMessage(byte[] bytes)
            {
                var newMessage = new byte[this.Message.Length + bytes.Length];
                Array.Copy(this.Message, newMessage, this.Message.Length);
                Array.Copy(bytes, 0, newMessage, this.Message.Length, bytes.Length);
            }
        }
    }
}
