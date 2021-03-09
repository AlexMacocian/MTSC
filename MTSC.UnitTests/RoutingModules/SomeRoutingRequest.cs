using MTSC.Common.Http.RoutingModules;

namespace MTSC.UnitTests.RoutingModules
{
    [RequestConvert(typeof(SomeRequestConverter))]
    public class SomeRoutingRequest
    {
    }
}
