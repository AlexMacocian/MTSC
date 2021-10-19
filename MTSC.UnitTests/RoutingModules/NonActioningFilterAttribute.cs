using MTSC.Common.Http;
using MTSC.Common.Http.Attributes;
using MTSC.ServerSide;

namespace MTSC.UnitTests.RoutingModules
{
    public sealed class NonActioningFilterAttribute : RouteFilterAttribute
    {
        public static bool RequestCalled { get; private set; }
        public static bool ResponseCalled { get; private set; }

        public override RouteEnablerResponse HandleRequest(Server server, ClientData clientData, HttpRequest httpRequest)
        {
            RequestCalled = true;
            return base.HandleRequest(server, clientData, httpRequest);
        }

        public override void HandleResponse(Server server, ClientData clientData, HttpResponse httpResponse)
        {
            ResponseCalled = true;
            base.HandleResponse(server, clientData, httpResponse);
        }
    }
}
