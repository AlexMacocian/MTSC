using System;

namespace MTSC.Exceptions
{
    public sealed class InvalidWebsocketFormatException : Exception
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
    }
}
