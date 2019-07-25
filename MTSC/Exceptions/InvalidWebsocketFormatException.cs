using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MTSC.Exceptions
{
    public class InvalidWebsocketFormatException : Exception
    {
        public InvalidWebsocketFormatException()
        {
        }

        public InvalidWebsocketFormatException(string message) : base(message)
        {
        }

        public InvalidWebsocketFormatException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidWebsocketFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
