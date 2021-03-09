using System;
using System.Linq;

namespace MTSC.Common.Http.RoutingModules
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RequestConvertAttribute : Attribute
    {
        public Type ConverterType { get; }

        public RequestConvertAttribute(Type type)
        {
            if (!type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestConverter<>)))
            {
                throw new InvalidOperationException($"{type.FullName} is not a {typeof(IRequestConverter<>).FullName}");
            }

            this.ConverterType = type;
        }
    }
}
