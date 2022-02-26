using MTSC.Common.Http.RoutingModules;
using Newtonsoft.Json;
using System;
using System.ComponentModel;

namespace MTSC.Common.Http.Attributes
{
    public abstract class StringRouteDataBindingBaseAttribute : RouteDataBindingBaseAttribute
    {
        public override sealed object DataBind(HttpRouteBase route, RouteContext routeContext, Type propertyType)
        {
            var stringValue = this.GetStringValue(route, routeContext);
            return BindValue(propertyType, stringValue);
        }

        /// <summary>
        /// Returns a string value that will later be converted to the decorated property <see cref="Type"/>.
        /// Conversion tries <see cref="TypeConverter"/> as well as <see cref="JsonConvert"/> if the property <see cref="Type"/> is not <see cref="string"/>.
        /// </summary>
        /// <param name="module"><see cref="HttpRouteBase"/> route.</param>
        /// <param name="routeContext"><see cref="RouteContext"/> of the request.</param>
        /// <returns>A string value to be converted to the decorated property <see cref="Type"/>.</returns>
        public abstract string GetStringValue(HttpRouteBase route, RouteContext routeContext);

        private object BindValue(Type propertyType, string value)
        {
            object finalValue = null;
            if (propertyType == typeof(string))
            {
                finalValue = value;
            }
            else if (this.TryConvertWithTypeConverter(propertyType, value, out var typeConvertedValue))
            {
                finalValue = typeConvertedValue;
            }
            else if (this.TryConvertWithJsonConvert(propertyType, value, out var jsonConvertedValue))
            {
                finalValue = jsonConvertedValue;
            }

            return finalValue;
        }

        private bool TryConvertWithTypeConverter(Type propertyType, string value, out object convertedValue)
        {
            var typeConverter = TypeDescriptor.GetConverter(propertyType);
            if (typeConverter.CanConvertFrom(typeof(string)))
            {
                convertedValue = typeConverter.ConvertFrom(value);
                return true;
            }

            convertedValue = null;
            return false;
        }

        private bool TryConvertWithJsonConvert(Type propertyType, string value, out object convertedValue)
        {
            convertedValue = JsonConvert.DeserializeObject(value, propertyType);
            return true;
        }
    }
}
