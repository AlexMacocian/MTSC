using System.ComponentModel;
using MTSC.Common.Http.RoutingModules;

namespace MTSC.UnitTests.RoutingModules
{
    [TypeConverter(typeof(SomeRequestConverter))]
    public class SomeRoutingRequest
    {
    }
}
