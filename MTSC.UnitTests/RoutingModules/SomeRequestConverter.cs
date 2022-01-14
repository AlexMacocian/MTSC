using System;
using System.ComponentModel;
using System.Globalization;
using MTSC.Common.Http;

namespace MTSC.UnitTests.RoutingModules
{
    public class SomeRequestConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(HttpRequestContext))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is HttpRequestContext)
            {
                return new SomeRoutingRequest();
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
