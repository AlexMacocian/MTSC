using MTSC.Server;
using System;

namespace MTSC.Common.Http.RoutingModules
{
    public class AlwaysEnabledRoutingEnabler : IRoutingEnabler
    {
        bool IRoutingEnabler.RouteEnabled(HttpRequest request, ClientData client)
        {
            return true;
        }
    }
}
