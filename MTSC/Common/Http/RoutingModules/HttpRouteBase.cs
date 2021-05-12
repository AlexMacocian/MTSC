using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;
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
        private readonly static object cachedLock = new object();
        private static IRequestConverter<T> CachedConverter { get; set; }

        public sealed override Task<HttpResponse> HandleRequest(HttpRequest request)
        {
            lock (cachedLock)
            {
                if (CachedConverter is null)
                {
                    CachedConverter = ImplementConverter();
                }
            }

            return this.HandleRequest(CachedConverter.ConvertHttpRequest(request));
        }

        public abstract Task<HttpResponse> HandleRequest(T request);

        private static bool MatchesRequiredType(RequestConvertAttribute attribute)
        {
            if (attribute.ConverterType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestConverter<T>)))
            {
                return false;
            }

            return true;
        }

        private static IRequestConverter<T> ImplementConverter()
        {
            var converterType = typeof(T)
                .GetCustomAttributes(true)
                .OfType<RequestConvertAttribute>()
                .Where(MatchesRequiredType)
                .Select(attribute => attribute.ConverterType)
                .FirstOrDefault();
            if (converterType is null)
            {
                throw new InvalidOperationException($"No converter found for type {typeof(T).FullName}");
            }

            var converter = Activator.CreateInstance(converterType) as IRequestConverter<T>;
            return converter;
        }
    }
    public abstract class HttpRouteBase<TReceive, TSend> : HttpRouteBase
    {
        private static readonly object reqLock = new object(), respLock = new object();
        private static IRequestConverter<TReceive> CachedRequestConverter { get; set; }
        private static IResponseConverter<TSend> CachedResponseConverter { get; set; }

        public sealed async override Task<HttpResponse> HandleRequest(HttpRequest request)
        {
            lock (reqLock)
            {
                if (CachedRequestConverter is null)
                {
                    CachedRequestConverter = ImplementRequestConverter();
                }
            }

            lock (respLock)
            {
                if (CachedResponseConverter is null)
                {
                    CachedResponseConverter = ImplementResponseConverter();
                }
            }

            return CachedResponseConverter.ConvertResponse(await this.HandleRequest(CachedRequestConverter.ConvertHttpRequest(request)));
        }

        public abstract Task<TSend> HandleRequest(TReceive request);

        private static bool MatchesRequiredRequestType(RequestConvertAttribute attribute)
        {
            if (attribute.ConverterType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestConverter<TReceive>)))
            {
                return false;
            }

            return true;
        }
        private static bool MatchesRequiredResponseType(ResponseConvertAttribute attribute)
        {
            if (attribute.ConverterType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IResponseConverter<TSend>)))
            {
                return false;
            }

            return true;
        }
        private static IResponseConverter<TSend> ImplementResponseConverter()
        {
            var converterType = typeof(TSend)
                .GetCustomAttributes(true)
                .OfType<ResponseConvertAttribute>()
                .Where(MatchesRequiredResponseType)
                .Select(attribute => attribute.ConverterType)
                .FirstOrDefault();
            if (converterType is null)
            {
                throw new InvalidOperationException($"No converter found for type {typeof(TSend).FullName}");
            }

            var converter = Activator.CreateInstance(converterType) as IResponseConverter<TSend>;
            return converter;
        }
        private static IRequestConverter<TReceive> ImplementRequestConverter()
        {
            var converterType = typeof(TReceive)
                .GetCustomAttributes(true)
                .OfType<RequestConvertAttribute>()
                .Where(MatchesRequiredRequestType)
                .Select(attribute => attribute.ConverterType)
                .FirstOrDefault();
            if (converterType is null)
            {
                throw new InvalidOperationException($"No converter found for type {typeof(TReceive).FullName}");
            }

            var converter = Activator.CreateInstance(converterType) as IRequestConverter<TReceive>;
            return converter;
        }
    }
}
