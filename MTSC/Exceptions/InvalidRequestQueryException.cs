using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public class InvalidRequestQueryException : Exception
    {
        public InvalidRequestQueryException()
        {
        }

        public InvalidRequestQueryException(string message) : base(message)
        {
        }

        public InvalidRequestQueryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidRequestQueryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
