using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;

namespace MTSC.UnitTests.RoutingModules
{
    public class SomeRequestConverter : IRequestConverter<SomeRoutingRequest>
    {
        public SomeRoutingRequest ConvertHttpRequest(HttpRequest httpRequest)
        {
            return new SomeRoutingRequest();
        }
    }
}
