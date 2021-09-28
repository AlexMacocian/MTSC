using System;
using System.ComponentModel;
using System.Globalization;
using MTSC.Common.Http;
using MTSC.Common.Http.RoutingModules;

namespace MTSC.UnitTests.RoutingModules
{
    public class SomeResponseConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(SomeRoutingResponse))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is SomeRoutingResponse)
            {
                return new HttpResponse { StatusCode = HttpMessage.StatusCodes.OK };
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
