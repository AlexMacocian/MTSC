using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception in case of first invalid byte for websocket communication.
    /// </summary>
    public class InvalidFirstByteException : Exception
    {
        public InvalidFirstByteException()
        {
        }

        public InvalidFirstByteException(string message) : base(message)
        {
        }

        public InvalidFirstByteException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidFirstByteException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
