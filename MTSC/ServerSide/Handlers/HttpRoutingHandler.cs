﻿using MTSC.Common;
using MTSC.Common.Http;
using MTSC.Common.Http.Attributes;
using MTSC.Common.Http.RoutingModules;
using MTSC.Common.Http.Telemetry;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.ServerSide.Handlers
{
    public sealed class HttpRoutingHandler : IHandler, IRunOnStartup
    {
        private static readonly Func<Server, HttpRequest, ClientData, RouteEnablerResponse> alwaysEnabled = (server, request, client) => RouteEnablerResponse.Accept;
        private readonly ConcurrentQueue<Tuple<ClientData, HttpResponse>> messageOutQueue = new();
        private readonly List<IHttpLogger> httpLoggers = new();
        private readonly Dictionary<HttpMethods, List<(ExtendedUrl, Type,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse>)>> moduleDictionary = new();

        public TimeSpan FragmentsExpirationTime { get; set; } = TimeSpan.FromSeconds(15);
        public double MaximumRequestSize { get; set; } = double.MaxValue;
        public bool Return500OnException { get; set; } = true;

        public HttpRoutingHandler()
        {
            foreach (var method in (HttpMethods[])Enum.GetValues(typeof(HttpMethods)))
            {
                this.moduleDictionary[method] = new List<(ExtendedUrl, Type, Func<Server, HttpRequest, ClientData, RouteEnablerResponse>)>();
            }
        }

        public HttpRoutingHandler WithReturn500OnException(bool return500OnException)
        {
            this.Return500OnException = return500OnException;
            return this;
        }
        public HttpRoutingHandler AddHttpLogger(IHttpLogger logger)
        {
            this.httpLoggers.Add(logger);
            return this;
        }
        public HttpRoutingHandler AddRoute<T>(
            HttpMethods method,
            string uri)
            where T : HttpRouteBase
        {
            this.RegisterRoute(method, uri, typeof(T), alwaysEnabled);
            return this;
        }
        public HttpRoutingHandler AddRoute<T>(
            HttpMethods method,
            string uri,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
            where T : HttpRouteBase
        {
            this.RegisterRoute(method, uri, typeof(T), routeEnabler);
            return this;
        }
        public HttpRoutingHandler AddRoute(
            HttpMethods method,
            string uri,
            Type routeType)
        {
            this.RegisterRoute(method, uri, routeType, alwaysEnabled);
            return this;
        }
        public HttpRoutingHandler AddRoute(
            HttpMethods method,
            string uri,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler,
            Type routeType)
        {
            this.RegisterRoute(method, uri, routeType, routeEnabler);
            return this;
        }
        public HttpRoutingHandler RemoveRoute(
            HttpMethods method,
            string uri)
        {
            this.moduleDictionary[method].Remove(this.moduleDictionary[method].Where(tuple => tuple.Item1.Url == uri).First());
            return this;
        }
        public HttpRoutingHandler WithMaximumSize(double size)
        {
            this.MaximumRequestSize = size;
            return this;
        }
        /// <summary>
        /// The amount of time fragments are kept in the buffer before being discarded.
        /// </summary>
        /// <param name="duration">Time until fragments expire.</param>
        /// <returns>This handler object.</returns>
        public HttpRoutingHandler WithFragmentsExpirationTime(TimeSpan duration)
        {
            this.FragmentsExpirationTime = duration;
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

        void IHandler.ClientRemoved(Server server, ClientData client) { }

        bool IHandler.HandleClient(Server server, ClientData client) => false;

        bool IHandler.HandleReceivedMessage(Server server, ClientData client, Message message)
        {
            /*
             * If a fragmented request exists, add the new messages to the body.
             * Else, parse the messages into a partial request. If this causes an exception, let it throw out of the handler,
             * cause the handler needs at least valid headers to work.
             */
            PartialHttpRequest request;
            if (client.Resources.TryGetResource<FragmentedMessage>(out var fragmentedMessage)) 
            {
                var bytesToBeAdded = message.MessageBytes.TrimTrailingNullBytes();
                if(fragmentedMessage.PartialRequest.HeaderByteCount + 
                    fragmentedMessage.PartialRequest.Body.Length + 
                    message.MessageLength > this.MaximumRequestSize)
                {
                    this.QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request exceeded [{this.MaximumRequestSize}] bytes!" });
                    client.ResetAffinityIfMe(this);
                    client.Resources.RemoveResource<FragmentedMessage>();
                    client.Resources.RemoveResourceIfExists<RequestMapping>();
                }

                fragmentedMessage.AddToMessage(bytesToBeAdded);
                request = fragmentedMessage.PartialRequest;
            }
            else
            {
                if (message.MessageLength > this.MaximumRequestSize)
                {
                    this.QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request exceeded [{this.MaximumRequestSize}] bytes!" });
                    return false;
                }

                request = PartialHttpRequest.FromBytes(message.MessageBytes.TrimTrailingNullBytes());
                if (!request.Complete)
                {
                    client.Resources.SetResource(new FragmentedMessage { LastReceived = DateTime.Now, PartialRequest = request });
                    client.SetAffinity(this);
                    if (request.Headers.ContainsHeader(RequestHeaders.Expect) &&
                        request.Headers[RequestHeaders.Expect].Equals("100-continue", StringComparison.OrdinalIgnoreCase))
                    {
                        server.LogDebug("Returning 100-Continue");
                        var contResponse = new HttpResponse { StatusCode = HttpMessage.StatusCodes.Continue };
                        contResponse.Headers[HttpMessage.GeneralHeaders.Connection] = "keep-alive";
                        this.QueueResponse(client, contResponse);
                    }
                }
            }

            /*
             * Once headers are loaded, check if mapping exists. If mapping exists between request and module,
             * verify that the request is complete and send it to module.
             * Otherwise, if the request is complete, send it to the mapped module. If the request is not complete,
             * handle it and return.
             */

            if (client.Resources.TryGetResource<RequestMapping>(out var mapping))
            {
                if (request.Complete)
                {
                    client.Resources.RemoveResource<RequestMapping>();
                    client.Resources.RemoveResource<FragmentedMessage>();
                    client.ResetAffinityIfMe(this);
                    this.HandleCompleteRequest(client, server, request.ToRequest(), mapping.MappedModule, mapping.UrlValues, mapping.RouteEnabler);
                    return true;
                }
                else
                {
                    this.HandleIncompleteRequest(server, client.Resources.GetResource<FragmentedMessage>());
                    return true;
                }
            }
            else
            {
                if (this.TryMatchUrl(request.Method, request.RequestURI, out var urlValues, out var routeType, out var routeEnabler))
                {
                    var module = this.GetRoute(routeType, client, server);
                    if (request.Complete)
                    {
                        var httpRequest = request.ToRequest();
                        client.ResetAffinityIfMe(this);
                        return this.HandleCompleteRequest(client, server, httpRequest, module, urlValues, routeEnabler);
                    }
                    else
                    {
                        client.Resources.SetResource(new RequestMapping { MappedModule = module, RouteEnabler = routeEnabler, UrlValues = urlValues });
                        return true;
                    }
                }
                else
                {
                    client.ResetAffinityIfMe(this);
                    client.Resources.RemoveResource<FragmentedMessage>();
                    return false;
                }
            }
        }

        bool IHandler.HandleSendMessage(Server server, ClientData client, ref Message message) => false;

        bool IHandler.PreHandleReceivedMessage(Server server, ClientData client, ref Message message) => false;

        void IHandler.Tick(Server server)
        {
            while (this.messageOutQueue.Count > 0)
            {
                if (this.messageOutQueue.TryDequeue(out var tuple))
                {
                    server.QueueMessage(tuple.Item1, tuple.Item2.GetPackedResponse(true));
                }
            }

            foreach (var client in server.Clients)
            {
                if (client.Resources.TryGetResource<FragmentedMessage>(out var fragmentedMessage)) 
                {
                    if ((DateTime.Now - fragmentedMessage.LastReceived) > this.FragmentsExpirationTime)
                    {
                        client.Resources.RemoveResource<FragmentedMessage>();
                        this.QueueResponse(client, new HttpResponse { StatusCode = StatusCodes.BadRequest, BodyString = $"Request timed out in [{this.FragmentsExpirationTime.TotalMilliseconds}] ms!" });
                    }
                }
            }
        }

        void IRunOnStartup.OnStartup(Server server)
        {
            foreach (var routes in this.moduleDictionary.Values)
            {
                foreach ((_, var routeType, _) in routes)
                {
                    server.ServiceManager.RegisterTransient(routeType, routeType);
                }
            }
        }

        private void HandleIncompleteRequest(Server server, FragmentedMessage fragmentedMessage)
        {
            fragmentedMessage.LastReceived = DateTime.Now;
            server.LogDebug("Incomplete request received!");
        }
        
        private bool HandleCompleteRequest(
            ClientData client, 
            Server server, 
            HttpRequest request, 
            HttpRouteBase module,
            List<UrlValue> urlValues,
            Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
        {
            foreach (var httpLogger in this.httpLoggers)
            {
                httpLogger.LogRequest(server, this, client, request);
            }

            var routeEnablerResponse = routeEnabler.Invoke(server, request, client);
            if (routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseAccept)
            {
                SetModuleProperties(module, request, urlValues);
                try
                {
                    module.CallHandleRequest(request).ContinueWith((task) => 
                    {
                        foreach (var httpLogger in this.httpLoggers)
                        {
                            httpLogger.LogResponse(server, this, client, task.Result);
                        }

                        this.QueueResponse(client, task.Result); 
                    });
                }
                catch (Exception e)
                {
                    server.LogDebug("Exception: " + e.Message);
                    server.LogDebug("Stacktrace: " + e.StackTrace);
                    var response = new HttpResponse() { StatusCode = StatusCodes.InternalServerError };
                    foreach (var httpLogger in this.httpLoggers)
                    {
                        httpLogger.LogResponse(server, this, client, response);
                    }

                    this.QueueResponse(client, response);
                }

                return true;
            }
            else if (routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseIgnore)
            {
                return false;
            }
            else if (routeEnablerResponse is RouteEnablerResponse.RouteEnablerResponseError)
            {
                foreach (var httpLogger in this.httpLoggers)
                {
                    httpLogger.LogResponse(server, this, client, (routeEnablerResponse as RouteEnablerResponse.RouteEnablerResponseError).Response);
                }

                this.QueueResponse(client, (routeEnablerResponse as RouteEnablerResponse.RouteEnablerResponseError).Response);
                return true;
            }
            else
            {
                throw new InvalidOperationException($"RouteEnablerResponse should be one of the types {typeof(RouteEnablerResponse.RouteEnablerResponseAccept)}, {typeof(RouteEnablerResponse.RouteEnablerResponseError)} or {typeof(RouteEnablerResponse.RouteEnablerResponseIgnore)}!");
            }
        }

        private class FragmentedMessage
        {
            public PartialHttpRequest PartialRequest { get; set; }

            public DateTime LastReceived { get; set; } = DateTime.Now;

            public void AddToMessage(byte[] bytes)
            {
                this.PartialRequest.AddToBody(bytes);
            }
        }

        private class RequestMapping
        {
            public List<UrlValue> UrlValues { get; set; }
            public HttpRouteBase MappedModule { get; set; }
            public Func<Server, HttpRequest, ClientData, RouteEnablerResponse> RouteEnabler { get; set; }
        }

        private HttpRouteBase GetRoute(Type routeType, ClientData client, Server server)
        {
            if (!typeof(HttpRouteBase).IsAssignableFrom(routeType))
            {
                throw new InvalidOperationException($"Cannot create new route of type {routeType.FullName}. Not of type {typeof(HttpRouteBase).FullName}");
            }

            var module = server.ServiceManager.GetService(routeType) as HttpRouteBase;
            (module as ISetHttpContext).SetClientData(client);
            (module as ISetHttpContext).SetServer(server);
            (module as ISetHttpContext).SetHttpRoutingHandler(this);

            return module;
        }

        private void RegisterRoute(HttpMethods method, string uri, Type routeType, Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
        {
            if (!typeof(HttpRouteBase).IsAssignableFrom(routeType))
            {
                throw new InvalidOperationException($"{routeType.FullName} must be of type {typeof(HttpRouteBase).FullName}");
            }

            this.moduleDictionary[method].Add((new ExtendedUrl(uri), routeType, routeEnabler));
        }

        private bool TryMatchUrl(HttpMethods method, string uri, out List<UrlValue> urlValues, out Type type, out Func<Server, HttpRequest, ClientData, RouteEnablerResponse> routeEnabler)
        {
            urlValues = null;
            type = null;
            routeEnabler = null;

            foreach((var url, var possibleType, var possibleRouteEnabler) in this.moduleDictionary[method])
            {
                if (url.TryMatchUrl(uri, out var matchedValues))
                {
                    urlValues = matchedValues;
                    type = possibleType;
                    routeEnabler = possibleRouteEnabler;
                    return true;
                }
            }

            return false;
        }

        private static void SetModuleProperties(HttpRouteBase module, HttpRequest httpRequest, List<UrlValue> urlValues)
        {
            var properties = module.GetType().GetProperties();
            foreach (var property in properties)
            {
                foreach (var attribute in property.GetCustomAttributes(true))
                {
                    if (attribute is FromUrlAttribute fromUrlAttribute)
                    {
                        var maybeValue = urlValues.Where(val => val.Placeholder == fromUrlAttribute.Placeholder).FirstOrDefault();
                        if (maybeValue is not null)
                        {
                            TryAssignValue(property, maybeValue.Value, module);
                        }
                    }
                    else if (attribute is FromBodyAttribute)
                    {
                        TryAssignValue(property, httpRequest.BodyString, module);
                    }
                    else if (attribute is FromHeadersAttribute fromHeadersAttribute)
                    {
                        var maybeValue = httpRequest.Headers.Where(kvp => kvp.Key == fromHeadersAttribute.HeaderName).FirstOrDefault();
                        if (maybeValue.Value is not null)
                        {
                            TryAssignValue(property, maybeValue.Value, module);
                        }
                    }
                }
            }
        }

        private static void TryAssignValue(PropertyInfo propertyInfo, string value, HttpRouteBase module)
        {
            object finalValue = null;
            if (propertyInfo.PropertyType == typeof(string))
            {
                finalValue = value;
            }
            else if (TryConvertWithTypeConverter(propertyInfo, value, out var typeConvertedValue))
            {
                finalValue = typeConvertedValue;
            }
            else if (TryConvertWithJsonConvert(propertyInfo, value, out var jsonConvertedValue))
            {
                finalValue = jsonConvertedValue;
            }

            SetPropertyValue(propertyInfo, finalValue, module);
        }

        private static void SetPropertyValue(PropertyInfo propertyInfo, object value, HttpRouteBase module)
        {
            if (propertyInfo.CanWrite is false)
            {
                var backingField = HelperFunctions.GetBackingField(propertyInfo);
                backingField.SetValue(module, value);
            }
            else
            {
                propertyInfo.SetValue(module, value);
            }
        }

        private static bool TryConvertWithTypeConverter(PropertyInfo propertyInfo, string value, out object convertedValue)
        {
            try
            {
                var typeConverter = TypeDescriptor.GetConverter(propertyInfo.PropertyType);
                if (typeConverter.CanConvertFrom(typeof(string)))
                {
                    convertedValue = typeConverter.ConvertFrom(value);
                    return true;
                }
            }
            catch
            {
            }

            convertedValue = null;
            return false;
        }
        
        private static bool TryConvertWithJsonConvert(PropertyInfo propertyInfo, string value, out object convertedValue)
        {
            try
            {
                convertedValue = JsonConvert.DeserializeObject(value, propertyInfo.PropertyType);
                return true;
            }
            catch
            {
            }

            convertedValue = null;
            return false;
        }
    }
}
