using System;
using System.Linq;

namespace MTSC.Common.Http.RoutingModules
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ResponseConvertAttribute : Attribute
    {
        public Type ConverterType { get; }

        public ResponseConvertAttribute(Type type)
        {
            if (!type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IResponseConverter<>)))
            {
                throw new InvalidOperationException($"{type.FullName} is not a {typeof(IResponseConverter<>).FullName}");
            }

            this.ConverterType = type;
        }
    }
}
