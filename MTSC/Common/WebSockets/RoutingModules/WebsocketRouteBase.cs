using MTSC.ServerSide;
using MTSC.ServerSide.Handlers;
using System;
using System.Linq;
using System.Text;

namespace MTSC.Common.WebSockets.RoutingModules
{
    public abstract class WebsocketRouteBase : ISetWebsocketContext
    {
        protected Server Server { get; private set; }
        protected WebsocketRoutingHandler WebsocketRoutingHandler { get; private set; }
        protected ClientData ClientData { get; private set; }

        public void CallConnectionInitialized()
        {
            this.ConnectionInitialized();
        }
        public void CallHandleReceivedMessage(WebsocketMessage receivedMessage)
        {
            this.HandleReceivedMessage(receivedMessage);
        }
        public void CallConnectionClosed()
        {
            this.ConnectionClosed();
        }
        public void SendMessage(WebsocketMessage message)
        {
            this.WebsocketRoutingHandler.QueueMessage(this.ClientData, message);
        }

        public abstract void ConnectionInitialized();
        public abstract void HandleReceivedMessage(WebsocketMessage receivedMessage);
        public abstract void ConnectionClosed();
        public abstract void Tick();

        void ISetWebsocketContext.SetServer(Server server)
        {
            this.Server = server;
        }
        void ISetWebsocketContext.SetHandler(WebsocketRoutingHandler websocketRoutingHandler)
        {
            this.WebsocketRoutingHandler = websocketRoutingHandler;
        }
        void ISetWebsocketContext.SetClient(ClientData clientData)
        {
            this.ClientData = clientData;
        }

        internal static IWebsocketMessageConverter<T> GetStringAdhocConverter<T>()
        {
            return new AdhocConverter<T>(
                        convertFrom: message => (T)(Encoding.UTF8.GetString(message.Data) as object),
                        convertTo: message =>
                        {
                            var str = (string)(message as object);
                            return new WebsocketMessage
                            {
                                Data = Encoding.UTF8.GetBytes(str),
                                Opcode = WebsocketMessage.Opcodes.Text
                            };
                        });
        }
        internal static IWebsocketMessageConverter<T> GetByteArrayAdhocConverter<T>()
        {
            return new AdhocConverter<T>(
                        convertFrom: message => (T)(message.Data as object),
                        convertTo: message =>
                        {
                            return new WebsocketMessage
                            {
                                Data = (byte[])(message as object),
                                Opcode = WebsocketMessage.Opcodes.Binary
                            };
                        });
        }
    }
    public abstract class WebsocketRouteBase<TReceive> : WebsocketRouteBase
    {
        private readonly static object cachedLock = new();
        private static IWebsocketMessageConverter<TReceive> CachedConverter { get; set; }

        public sealed override void HandleReceivedMessage(WebsocketMessage receivedMessage)
        {
            lock (cachedLock)
            {
                if (CachedConverter is null)
                {
                    CachedConverter = ImplementConverter();
                }
            }

            this.HandleReceivedMessage(CachedConverter.ConvertFromWebsocketMessage(receivedMessage));
        }
        public abstract void HandleReceivedMessage(TReceive message);

        private static bool MatchesRequiredType(WebsocketMessageConvertAttribute attribute)
        {
            if (attribute.ConverterType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IWebsocketMessageConverter<TReceive>)))
            {
                return false;
            }

            return true;
        }
        private static IWebsocketMessageConverter<TReceive> ImplementConverter()
        {
            var converterType = typeof(TReceive)
                .GetCustomAttributes(true)
                .OfType<WebsocketMessageConvertAttribute>()
                .Where(MatchesRequiredType)
                .Select(attribute => attribute.ConverterType)
                .FirstOrDefault();
            if (converterType is null)
            {
                if (typeof(TReceive) == typeof(string))
                {
                    return GetStringAdhocConverter<TReceive>();
                }
                else if (typeof(TReceive) == typeof(byte[]))
                {
                    return GetByteArrayAdhocConverter<TReceive>();
                }

                throw new InvalidOperationException($"No converter found for type {typeof(TReceive).FullName}");
            }

            var converter = Activator.CreateInstance(converterType) as IWebsocketMessageConverter<TReceive>;
            return converter;
        }
    }
    public abstract class WebsocketRouteBase<TReceive, TSend> : WebsocketRouteBase
    {
        private readonly static object recLock = new(), sendLock = new();
        private static IWebsocketMessageConverter<TReceive> CachedReceiveConverter { get; set; }
        private static IWebsocketMessageConverter<TSend> CachedSendConverter { get; set; }

        public void SendMessage(TSend message)
        {
            lock (sendLock)
            {
                if (CachedSendConverter is null)
                {
                    CachedSendConverter = ImplementSendConverter();
                }
            }

            base.SendMessage(CachedSendConverter.ConvertToWebsocketMessage(message));
        }
        public sealed override void HandleReceivedMessage(WebsocketMessage receivedMessage)
        {
            lock (recLock)
            {
                if (CachedReceiveConverter is null)
                {
                    CachedReceiveConverter = ImplementReceiveConverter();
                }
            }
            
            this.HandleReceivedMessage(CachedReceiveConverter.ConvertFromWebsocketMessage(receivedMessage));
        }
        public abstract void HandleReceivedMessage(TReceive message);

        private static bool MatchesRequiredReceiveType(WebsocketMessageConvertAttribute attribute)
        {
            if (attribute.ConverterType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IWebsocketMessageConverter<TReceive>)))
            {
                return false;
            }

            return true;
        }
        private static bool MatchesRequiredSendType(WebsocketMessageConvertAttribute attribute)
        {
            if (attribute.ConverterType.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IWebsocketMessageConverter<TSend>)))
            {
                return false;
            }

            return true;
        }
        private static IWebsocketMessageConverter<TSend> ImplementSendConverter()
        {
            var converterType = typeof(TSend)
                .GetCustomAttributes(true)
                .OfType<WebsocketMessageConvertAttribute>()
                .Where(MatchesRequiredSendType)
                .Select(attribute => attribute.ConverterType)
                .FirstOrDefault();
            if (converterType is null)
            {
                if (typeof(TSend) == typeof(string))
                {
                    return GetStringAdhocConverter<TSend>();
                }
                else if (typeof(TSend) == typeof(byte[]))
                {
                    return GetByteArrayAdhocConverter<TSend>();
                }

                throw new InvalidOperationException($"No converter found for type {typeof(TSend).FullName}");
            }

            var converter = Activator.CreateInstance(converterType) as IWebsocketMessageConverter<TSend>;
            return converter;
        }
        private static IWebsocketMessageConverter<TReceive> ImplementReceiveConverter()
        {
            var converterType = typeof(TReceive)
                .GetCustomAttributes(true)
                .OfType<WebsocketMessageConvertAttribute>()
                .Where(MatchesRequiredReceiveType)
                .Select(attribute => attribute.ConverterType)
                .FirstOrDefault();
            if (converterType is null)
            {
                if (typeof(TReceive) == typeof(string))
                {
                    return GetStringAdhocConverter<TReceive>();
                }
                else if (typeof(TReceive) == typeof(byte[]))
                {
                    return GetByteArrayAdhocConverter<TReceive>();
                }

                throw new InvalidOperationException($"No converter found for type {typeof(TReceive).FullName}");
            }

            var converter = Activator.CreateInstance(converterType) as IWebsocketMessageConverter<TReceive>;
            return converter;
        }
    }
}
