using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception cause by an invalid request URI.
    /// </summary>
    public class InvalidRequestURIException : Exception
    {
        public InvalidRequestURIException()
        {
        }

        public InvalidRequestURIException(string message) : base(message)
        {
        }

        public InvalidRequestURIException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidRequestURIException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
