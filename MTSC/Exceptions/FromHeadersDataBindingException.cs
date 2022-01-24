using System;

namespace MTSC.Exceptions
{
    public sealed class FromHeadersDataBindingException : Exception
    {
        public Type PropertyType { get; }
        public string Header { get; }
        public string Value { get; }

        public FromHeadersDataBindingException(
            Exception innerException,
            Type propertyType,
            string header,
            string value)
            : base("Encountered exception when binding data from headers", innerException)
        {
            this.PropertyType = propertyType;
            this.Header = header;
            this.Value = value;
        }
    }
}
