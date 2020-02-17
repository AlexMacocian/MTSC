using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Invalid HTTP Method Exception.
    /// </summary>
    public class InvalidMethodException : Exception
    {
        public InvalidMethodException()
        {
        }

        public InvalidMethodException(string message) : base(message)
        {
        }

        public InvalidMethodException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidMethodException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
