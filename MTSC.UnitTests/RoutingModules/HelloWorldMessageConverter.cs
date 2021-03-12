using MTSC.Common.WebSockets;
using MTSC.Common.WebSockets.RoutingModules;
using System.Text;

namespace MTSC.UnitTests.RoutingModules
{
    public class HelloWorldMessageConverter : IWebsocketMessageConverter<HelloWorldMessage>
    {
        public HelloWorldMessage ConvertFromWebsocketMessage(WebsocketMessage websocketMessage)
        {
            var str = Encoding.UTF8.GetString(websocketMessage.Data);
            return new HelloWorldMessage
            {
                HelloWorld = str == "Hello world!"
            };
        }

        public WebsocketMessage ConvertToWebsocketMessage(HelloWorldMessage message)
        {
            return new WebsocketMessage
            {
                Data = message.HelloWorld ? Encoding.UTF8.GetBytes("Hello world!") : Encoding.UTF8.GetBytes("Not hello world!"),
                Opcode = WebsocketMessage.Opcodes.Text
            };
        }
    }
}
