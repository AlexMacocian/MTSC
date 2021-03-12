using System;

namespace MTSC.Common.WebSockets.RoutingModules
{
    internal class AdhocConverter<T> : IWebsocketMessageConverter<T>
    {
        private readonly Func<WebsocketMessage, T> convertFrom;
        private readonly Func<T, WebsocketMessage> convertTo;

        public AdhocConverter(Func<WebsocketMessage, T> convertFrom, Func<T, WebsocketMessage> convertTo)
        {
            this.convertFrom = convertFrom;
            this.convertTo = convertTo;
        }

        public T ConvertFromWebsocketMessage(WebsocketMessage websocketMessage)
        {
            return this.convertFrom(websocketMessage);
        }

        public WebsocketMessage ConvertToWebsocketMessage(T message)
        {
            return this.convertTo(message);
        }
    }
}
