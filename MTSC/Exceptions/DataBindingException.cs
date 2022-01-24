using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public abstract class DataBindingException : Exception
    {
        protected DataBindingException()
        {
        }

        protected DataBindingException(string message) : base(message)
        {
        }

        protected DataBindingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DataBindingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
