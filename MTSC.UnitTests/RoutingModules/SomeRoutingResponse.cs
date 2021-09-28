using System.ComponentModel;
using MTSC.Common.Http.RoutingModules;

namespace MTSC.UnitTests.RoutingModules
{
    [TypeConverter(typeof(SomeResponseConverter))]
    public class SomeRoutingResponse
    {
    }
}
