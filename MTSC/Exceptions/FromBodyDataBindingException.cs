using System;

namespace MTSC.Exceptions
{
    public sealed class FromBodyDataBindingException : Exception
    {
        public Type PropertyType { get; }
        public string Value { get; }

        public FromBodyDataBindingException(
            Exception innerException,
            Type propertyType,
            string value)
            : base("Encountered exception when binding data from body", innerException)
        {
            this.PropertyType = propertyType;
            this.Value = value;
        }
    }
}
