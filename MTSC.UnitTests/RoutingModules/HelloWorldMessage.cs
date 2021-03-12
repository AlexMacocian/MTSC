using MTSC.Common.WebSockets.RoutingModules;

namespace MTSC.UnitTests.RoutingModules
{
    [WebsocketMessageConvert(typeof(HelloWorldMessageConverter))]
    public class HelloWorldMessage
    {
        public bool HelloWorld { get; set; }
    }
}
