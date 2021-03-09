using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.ServerSide;
using System.Threading.Tasks;
using System.Web;

namespace MTSC.UnitTests
{
    class TestQueryModule : HttpRouteBase
    {
        public override Task<HttpResponse> HandleRequest(HttpRequest request)
        {
            var query = HttpUtility.ParseQueryString(request.RequestQuery);
            if(query.Count == 2 &&
                query.Keys[0] == "key1" &&
                query.Keys[1] == "key2")
            {
                return Task.FromResult(new HttpResponse { StatusCode = HttpMessage.StatusCodes.OK });
            }
            else
            {
                return Task.FromResult(new HttpResponse { StatusCode = HttpMessage.StatusCodes.BadRequest });
            }
        }
    }
}
