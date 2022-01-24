using System;

namespace MTSC.Exceptions
{
    public sealed class FromUrlDataBindingException : DataBindingException
    {
        public Type PropertyType { get; }
        public string Placeholder { get; }
        public string Value { get; }

        public FromUrlDataBindingException(
            Exception innerException,
            Type propertyType,
            string placeHolder,
            string value)
            : base("Encountered exception when binding data from url", innerException)
        {
            this.PropertyType = propertyType;
            this.Placeholder = placeHolder;
            this.Value = value;
        }
    }
}
