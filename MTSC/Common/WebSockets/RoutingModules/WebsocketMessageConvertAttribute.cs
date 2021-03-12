using System;
using System.Linq;

namespace MTSC.Common.WebSockets.RoutingModules
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class WebsocketMessageConvertAttribute : Attribute
    {
        public Type ConverterType { get; }

        public WebsocketMessageConvertAttribute(Type type)
        {
            if (!type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IWebsocketMessageConverter<>)))
            {
                throw new InvalidOperationException($"{type.FullName} is not a {typeof(IWebsocketMessageConverter<>).FullName}");
            }

            this.ConverterType = type;
        }
    }
}
