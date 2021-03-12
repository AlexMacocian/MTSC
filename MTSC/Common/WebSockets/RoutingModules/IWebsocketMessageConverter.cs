namespace MTSC.Common.WebSockets.RoutingModules
{
    public interface IWebsocketMessageConverter<T>
    {
        T ConvertFromWebsocketMessage(WebsocketMessage websocketMessage);
        WebsocketMessage ConvertToWebsocketMessage(T message);
    }
}
