using MTSC.Common.Http.RoutingModules;

namespace MTSC.UnitTests.RoutingModules
{
    [ResponseConvert(typeof(SomeResponseConverter))]
    public class SomeRoutingResponse
    {
    }
}
