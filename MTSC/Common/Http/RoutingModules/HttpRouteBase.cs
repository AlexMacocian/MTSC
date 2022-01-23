using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace MTSC.Common.Http.RoutingModules
{
    public abstract class HttpRouteBase : ISetHttpContext, IDisposable
    {
        private bool disposedValue;

        public ClientData ClientData { get; private set; }
        public HttpRoutingHandler HttpRoutingHandler { get; private set; }
        public Server Server { get; private set; }
        public Slim.IServiceProvider ScopedServiceProvider { get; private set; }

        public async Task<HttpResponse> CallHandleRequest(HttpRequest request)
        {
            try
            {
                var context = new HttpRequestContext(
                    clientData: this.ClientData,
                    httpRequest: request,
                    httpRouteBase: this,
                    httpRoutingHandler: this.HttpRoutingHandler);
                return await this.HandleRequest(context);
            }
            catch
            {
                if (this.HttpRoutingHandler.Return500OnUnhandledException is true)
                {
                    return this.InternalServerError500;
                }

                throw;
            }
        }
        public abstract Task<HttpResponse> HandleRequest(HttpRequestContext request);

        protected HttpResponse Ok200 => CreateResponse(HttpMessage.StatusCodes.OK);
        protected HttpResponse Ok200WithPayload(object payload) => CreateResponse(HttpMessage.StatusCodes.OK, payload);
        protected HttpResponse Created201 => CreateResponse(HttpMessage.StatusCodes.Created);
        protected HttpResponse Created201WithPayload(object payload) => CreateResponse(HttpMessage.StatusCodes.Created, payload);
        protected HttpResponse Accepted202 => CreateResponse(HttpMessage.StatusCodes.Accepted);
        protected HttpResponse Accepted202WithPayload(object payload) => CreateResponse(HttpMessage.StatusCodes.Accepted, payload);
        protected HttpResponse BadRequest400 => CreateResponse(HttpMessage.StatusCodes.BadRequest);
        protected HttpResponse BadRequest400WithPayload(object payload) => CreateResponse(HttpMessage.StatusCodes.BadRequest, payload);
        protected HttpResponse Unauthorized401 => CreateResponse(HttpMessage.StatusCodes.Unauthorized);
        protected HttpResponse Unauthorized401WithPayload(object payload) => CreateResponse(HttpMessage.StatusCodes.Unauthorized, payload);
        protected HttpResponse Forbidden403 => CreateResponse(HttpMessage.StatusCodes.Forbidden);
        protected HttpResponse Forbidden403WithPayload(object payload) => CreateResponse(HttpMessage.StatusCodes.Forbidden, payload);
        protected HttpResponse NotFound404 => CreateResponse(HttpMessage.StatusCodes.NotFound);
        protected HttpResponse NotFound404WithPayload(object payload) => CreateResponse(HttpMessage.StatusCodes.NotFound, payload);
        protected HttpResponse Gone410 => CreateResponse(HttpMessage.StatusCodes.Gone);
        protected HttpResponse Gone410WithPayload(object payload) => CreateResponse(HttpMessage.StatusCodes.Gone, payload);
        protected HttpResponse InternalServerError500 => CreateResponse(HttpMessage.StatusCodes.InternalServerError);
        protected HttpResponse InternalServerError500WithPayload(object payload) => CreateResponse(HttpMessage.StatusCodes.InternalServerError, payload);

        void ISetHttpContext.SetScopedServiceProvider(Slim.IServiceProvider serviceProvider)
        {
            this.ScopedServiceProvider = serviceProvider;
        }
        void ISetHttpContext.SetClientData(ClientData clientData)
        {
            this.ClientData = clientData;
        }
        void ISetHttpContext.SetHttpRoutingHandler(HttpRoutingHandler httpRoutingHandler)
        {
            this.HttpRoutingHandler = httpRoutingHandler;
        }
        void ISetHttpContext.SetServer(Server server)
        {
            this.Server = server;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                this.disposedValue = true;
                if (disposing)
                {
                    this.ScopedServiceProvider.Dispose();
                }

                this.ScopedServiceProvider = null;
            }
        }
        public void Dispose()
        {
            this.Dispose(disposing: true);
        }

        private static HttpResponse CreateResponse(HttpMessage.StatusCodes statusCode, object payload = null) => new()
        {
            StatusCode = statusCode,
            BodyString = payload is null ? string.Empty :
                payload is string stringPayload ? stringPayload :    
                    JsonConvert.SerializeObject(payload)
        };
    }
    public abstract class HttpRouteBase<T> : HttpRouteBase
    {
        public sealed override Task<HttpResponse> HandleRequest(HttpRequestContext request)
        {
            var typeConverter = TypeDescriptor.GetConverter(typeof(T));
            return this.HandleRequest((T)typeConverter.ConvertFrom(request));
        }

        public abstract Task<HttpResponse> HandleRequest(T request);
    }
    public abstract class HttpRouteBase<TReceive, TSend> : HttpRouteBase
    {
        public sealed async override Task<HttpResponse> HandleRequest(HttpRequestContext request)
        {
            var requestTypeConverter = TypeDescriptor.GetConverter(typeof(TReceive));
            var responseTypeConverter = TypeDescriptor.GetConverter(typeof(TSend));
            return (HttpResponse)responseTypeConverter.ConvertTo(await this.HandleRequest((TReceive)requestTypeConverter.ConvertFrom(request)), typeof(HttpResponse));
        }

        public abstract Task<TSend> HandleRequest(TReceive request);
    }
}
