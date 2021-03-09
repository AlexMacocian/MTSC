namespace MTSC.Common.Http.RoutingModules
{
    public interface IResponseConverter<T>
    {
        HttpResponse ConvertResponse(T response);
    }
}
