using MTSC.Common;
using MTSC.Common.Http;
using MTSC.Common.Http.Attributes;
using MTSC.Common.Http.RoutingModules;
using MTSC.Common.Http.Telemetry;
using MTSC.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static MTSC.Common.Http.HttpMessage;

namespace MTSC.ServerSide.Handlers
{
    public sealed class HttpRoutingHandler : IHandler, IRunOnStartup
    {
        private class FragmentedMessage
        {
            public PartialHttpRequest PartialRequest { get; set; }

            public DateTime LastReceived { get; set; } = DateTime.Now;

            public void AddToMessage(byte[] bytes)
            {
                this.PartialRequest.AppendBytes(bytes);
            }
        }

        private class RequestMapping
        {
            public List<UrlValue> UrlValues { get; set; }
            public HttpRouteBase MappedModule { get; set; }
            public List<Type> FilterTypes { get; set; }
        }

        private readonly ConcurrentQueue<Tuple<ClientData, HttpResponse>> messageOutQueue = new();
        private readonly List<IHttpLogger> httpLoggers = new();
        private readonly Dictionary<Type, List<(Attribute, PropertyInfo)>> routePropertyCache = new();
        // List containing a tuple of url of module, type of module and list of types of filters for the module.
        private readonly Dictionary<HttpMethods, List<(ExtendedUrl, Type, List<Type>)>> moduleDictionary = new();

        public TimeSpan FragmentsExpirationTime { get; set; } = TimeSpan.FromSeconds(15);
        public double MaximumRequestSize { get; set; } = double.MaxValue;
        public bool Return500OnUnhandledException { get; set; } = true;
        public bool Return404OnNotFound { get; set; } = false;
        /// <summary>
        /// Throw when exceptions happen during request model bindings.
        /// </summary>
        public bool ThrowOnBindingErrors { get; set; } = false;

        public HttpRoutingHandler()
        {
            foreach (var method in (HttpMethods[])Enum.GetValues(typeof(HttpMethods)))
            {
                this.moduleDictionary[method] = new List<(ExtendedUrl, Type, List<Type>)>();
            }
        }

        public HttpRoutingHandler WithThrowOnBindingErrors(bool throwOnBindingErrors)
        {
            this.ThrowOnBindingErrors = throwOnBindingErrors;
            return this;
        }
        public HttpRoutingHandler WithReturn404OnNotFound(bool return404OnNotFound)
        {
            this.Return404OnNotFound = return404OnNotFound;
            return this;
        }
        public HttpRoutingHandler WithReturn500OnUnhandledException(bool return500OnException)
        {
            this.Return500OnUnhandledException = return500OnException;
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
            this.RegisterRoute(method, uri, typeof(T));
            return this;
        }
        public HttpRoutingHandler AddRoute(
            HttpMethods method,
            string uri,
            Type routeType)
        {
            this.RegisterRoute(method, uri, routeType);
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
            try
            {
                return this.HandleMessageInternal(client, server, message);
            }
            catch(Exception)
            {
                client.ResetAffinityIfMe(this);
                client.Resources.RemoveResource<FragmentedMessage>();
                if (this.Return500OnUnhandledException)
                {
                    var response = InternalServerError500;
                    foreach (var httpLogger in this.httpLoggers)
                    {
                        httpLogger.LogResponse(server, this, client, response);
                    }

                    this.QueueResponse(client, response);
                }

                throw;
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
                    foreach (var httpLogger in this.httpLoggers)
                    {
                        httpLogger.LogResponse(server, this, tuple.Item1, tuple.Item2);
                    }

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
                foreach ((_, var routeType, var filterTypes) in routes)
                {
                    server.ServiceManager.RegisterTransient(routeType, routeType);
                    foreach(var routeAttribute in routeType.GetCustomAttributes(true).Cast<Attribute>()
                        .Where(attr => typeof(RouteFilterAttribute).IsAssignableFrom(attr.GetType())))
                    {
                        filterTypes.Add(routeAttribute.GetType());
                        if (server.ServiceManager.IsRegistered(routeAttribute.GetType()) is false)
                        {
                            server.ServiceManager.RegisterScoped(routeAttribute.GetType());
                        }
                    }

                    this.PrepareRoutePropertyCache(routeType);
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
            List<Type> filterTypes)
        {
            foreach (var httpLogger in this.httpLoggers)
            {
                httpLogger.LogRequest(server, this, client, request);
            }

            var routeContext = new RouteContext(
                server,
                request,
                client,
                module.ScopedServiceProvider,
                urlValues.ToDictionary(u => u.Placeholder, u => u.Value));
            foreach(var filterType in filterTypes)
            {
                var filter = module.ScopedServiceProvider.GetService(filterType) as RouteFilterAttribute;
                var filterResponse = filter.HandleRequest(routeContext);
                if (filterResponse is RouteEnablerResponse.RouteEnablerResponseAccept)
                {
                    continue;
                }
                else if (filterResponse is RouteEnablerResponse.RouteEnablerResponseIgnore)
                {
                    return false;
                }
                else if (filterResponse is RouteEnablerResponse.RouteEnablerResponseError errorResponse)
                {
                    this.QueueResponse(client, errorResponse.Response);
                    return true;
                }
                else
                {
                    throw new InvalidOperationException($"RouteEnablerResponse should be one of the types {typeof(RouteEnablerResponse.RouteEnablerResponseAccept)}, {typeof(RouteEnablerResponse.RouteEnablerResponseError)} or {typeof(RouteEnablerResponse.RouteEnablerResponseIgnore)}!");
                }
            }

            
            this.RouteHandleRequest(module, routeContext, filterTypes).ContinueWith(task =>
            {
                this.QueueResponse(client, task.Result);
            });

            return true;
        }

        private bool HandleMessageInternal(ClientData client, Server server, Message message)
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
                if (fragmentedMessage.PartialRequest.BufferLength +
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
                    this.HandleCompleteRequest(client, server, request.ToRequest(), mapping.MappedModule, mapping.UrlValues, mapping.FilterTypes);
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
                if (this.TryMatchUrl(request.Method, request.RequestURI, out var urlValues, out var routeType, out var filterTypes))
                {
                    var scopedServiceProvider = server.ServiceManager.CreateScope();
                    var module = this.GetRoute(scopedServiceProvider, routeType, client, server);
                    if (request.Complete)
                    {
                        var httpRequest = request.ToRequest();
                        client.ResetAffinityIfMe(this);
                        return this.HandleCompleteRequest(client, server, httpRequest, module, urlValues, filterTypes);
                    }
                    else
                    {
                        client.Resources.SetResource(new RequestMapping { MappedModule = module, UrlValues = urlValues, FilterTypes = filterTypes });
                        return true;
                    }
                }
                else
                {
                    client.ResetAffinityIfMe(this);
                    client.Resources.RemoveResource<FragmentedMessage>();
                    if (this.Return404OnNotFound)
                    {
                        this.QueueResponse(client, NotFound404);
                    }

                    return false;
                }
            }
        }

        private async Task<HttpResponse> RouteHandleRequest(
            HttpRouteBase httpRouteBase,
            RouteContext routeContext,
            List<Type> filterTypes)
        {
            try
            {
                this.SetModuleProperties(httpRouteBase, routeContext);
                var response = await httpRouteBase.CallHandleRequest(routeContext.HttpRequest);
                routeContext.HttpResponse = response;
                foreach (var filterType in filterTypes)
                {
                    var filter = httpRouteBase.ScopedServiceProvider.GetService(filterType) as RouteFilterAttribute;
                    filter.HandleResponse(routeContext);
                }

                return response;
            }
            catch (Exception ex)
            {
                foreach (var filterType in filterTypes)
                {
                    var filter = httpRouteBase.ScopedServiceProvider.GetService(filterType) as RouteFilterAttribute;
                    if (filter.HandleException(routeContext, ex) is RouteFilterExceptionHandlingResponse.HandledResponse handledExceptionResponse)
                    {
                        routeContext.HttpResponse = handledExceptionResponse.HttpResponse;
                        return handledExceptionResponse.HttpResponse;
                    }
                }

                throw;
            }
        }

        private HttpRouteBase GetRoute(Slim.IServiceProvider serviceProvider, Type routeType, ClientData client, Server server)
        {
            if (!typeof(HttpRouteBase).IsAssignableFrom(routeType))
            {
                throw new InvalidOperationException($"Cannot create new route of type {routeType.FullName}. Not of type {typeof(HttpRouteBase).FullName}");
            }

            var module = serviceProvider.GetService(routeType) as HttpRouteBase;
            (module as ISetHttpContext).SetClientData(client);
            (module as ISetHttpContext).SetServer(server);
            (module as ISetHttpContext).SetHttpRoutingHandler(this);
            (module as ISetHttpContext).SetScopedServiceProvider(serviceProvider);
            client.Resources.SetResource(module);

            return module;
        }

        private void RegisterRoute(HttpMethods method, string uri, Type routeType)
        {
            if (!typeof(HttpRouteBase).IsAssignableFrom(routeType))
            {
                throw new InvalidOperationException($"{routeType.FullName} must be of type {typeof(HttpRouteBase).FullName}");
            }

            this.moduleDictionary[method].Add((new ExtendedUrl(uri), routeType, new List<Type>()));
        }

        private void PrepareRoutePropertyCache(Type routeType)
        {
            if (this.routePropertyCache.ContainsKey(routeType))
            {
                return;
            }

            var properties = routeType.GetProperties();
            var propertyAndAttributesList = new List<(Attribute, PropertyInfo)>();
            foreach (var property in properties)
            {
                foreach (var attribute in property.GetCustomAttributes(true))
                {
                    if (attribute is RouteDataBindingBaseAttribute)
                    {
                        propertyAndAttributesList.Add(((Attribute)attribute, property));
                    }
                }
            }

            this.routePropertyCache[routeType] = propertyAndAttributesList;
        }

        private bool TryMatchUrl(HttpMethods method, string uri, out List<UrlValue> urlValues, out Type type, out List<Type> filters)
        {
            urlValues = null;
            type = null;
            filters = null;

            foreach((var url, var possibleType, var possibleFilterTypes) in this.moduleDictionary[method])
            {
                if (url.TryMatchUrl(uri, out var matchedValues))
                {
                    filters = possibleFilterTypes;
                    urlValues = matchedValues;
                    type = possibleType;
                    return true;
                }
            }

            return false;
        }

        private void SetModuleProperties(HttpRouteBase module, RouteContext routeContext)
        {
            foreach ((var attribute, var propertyInfo) in this.routePropertyCache[module.GetType()])
            {
                if (attribute is RouteDataBindingBaseAttribute routeDataBindingBaseAttribute)
                {
                    try
                    {
                        var value = routeDataBindingBaseAttribute.DataBind(module, routeContext, propertyInfo.PropertyType);
                        SetPropertyValue(propertyInfo, value, module);
                    }
                    catch (Exception ex)
                    {
                        if (this.ThrowOnBindingErrors)
                        {
                            throw new DataBindingException(ex, module, routeContext, routeDataBindingBaseAttribute, propertyInfo);
                        }
                    }
                }
            }
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

        

        private static HttpResponse NotFound404 => new()
        {
            StatusCode = StatusCodes.NotFound
        };
        private static HttpResponse InternalServerError500 => new()
        {
            StatusCode = StatusCodes.InternalServerError
        };
    }
}
