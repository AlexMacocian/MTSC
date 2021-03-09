namespace MTSC.Common.Http.RoutingModules
{
    public interface IRequestConverter<T>
    {
        T ConvertHttpRequest(HttpRequest httpRequest);
    }
}
