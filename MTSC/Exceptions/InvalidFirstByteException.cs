using System;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception in case of first invalid byte for websocket communication.
    /// </summary>
    public sealed class InvalidFirstByteException : Exception
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
    }
}
