using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;
using MTSC.Server;
using System.Web;

namespace MTSC.UnitTests
{
    class TestQueryModule : HttpRouteBase
    {
        public override HttpResponse HandleRequest(HttpRequest request, ClientData client, Server.Server server)
        {
            var query = HttpUtility.ParseQueryString(request.RequestQuery);
            if(query.Count == 2 &&
                query.Keys[0] == "key1" &&
                query.Keys[1] == "key2")
            {
                return new HttpResponse { StatusCode = HttpMessage.StatusCodes.OK };
            }
            else
            {
                return new HttpResponse { StatusCode = HttpMessage.StatusCodes.BadRequest };
            }
        }
    }
}
