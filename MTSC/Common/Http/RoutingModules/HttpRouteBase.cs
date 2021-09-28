using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace MTSC.Common.Http.RoutingModules
{
    public abstract class HttpRouteBase : ISetHttpContext
    {
        private static HttpResponse InternalServerError500 { get; } =
            new HttpResponse
            {
                StatusCode = HttpMessage.StatusCodes.InternalServerError,
                BodyString = "An exception ocurred while processing the request"
            };

        public ClientData ClientData { get; private set; }
        public HttpRoutingHandler HttpRoutingHandler { get; private set; }
        public Server Server { get; private set; }

        public async Task<HttpResponse> CallHandleRequest(HttpRequest request)
        {
            try
            {
                return await this.HandleRequest(request);
            }
            catch
            {
                if (this.HttpRoutingHandler.Return500OnException is true)
                {
                    return InternalServerError500;
                }

                throw;
            }
        }
        public abstract Task<HttpResponse> HandleRequest(HttpRequest request);

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
    }
    public abstract class HttpRouteBase<T> : HttpRouteBase
    {
        public sealed override Task<HttpResponse> HandleRequest(HttpRequest request)
        {
            var typeConverter = TypeDescriptor.GetConverter(typeof(T));
            return this.HandleRequest((T)typeConverter.ConvertFrom(request));
        }

        public abstract Task<HttpResponse> HandleRequest(T request);
    }
    public abstract class HttpRouteBase<TReceive, TSend> : HttpRouteBase
    {
        public sealed async override Task<HttpResponse> HandleRequest(HttpRequest request)
        {
            var requestTypeConverter = TypeDescriptor.GetConverter(typeof(TReceive));
            var responseTypeConverter = TypeDescriptor.GetConverter(typeof(TSend));
            return (HttpResponse)responseTypeConverter.ConvertTo(await this.HandleRequest((TReceive)requestTypeConverter.ConvertFrom(request)), typeof(HttpResponse));
        }

        public abstract Task<TSend> HandleRequest(TReceive request);
    }
}
