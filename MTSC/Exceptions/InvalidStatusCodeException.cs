using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception for invalid response status code.
    /// </summary>
    public class InvalidStatusCodeException : Exception
    {
        public InvalidStatusCodeException()
        {
        }

        public InvalidStatusCodeException(string message) : base(message)
        {
        }

        public InvalidStatusCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidStatusCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
