using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;

namespace MTSC.UnitTests.RoutingModules
{
    public class SomeResponseConverter : IResponseConverter<SomeRoutingResponse>
    {
        public HttpResponse ConvertResponse(SomeRoutingResponse response)
        {
            return new HttpResponse { StatusCode = HttpMessage.StatusCodes.OK };
        }
    }
}
