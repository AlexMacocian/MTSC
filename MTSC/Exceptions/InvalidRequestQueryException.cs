using System;

namespace MTSC.Exceptions
{
    public sealed class InvalidRequestQueryException : Exception
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
    }
}
