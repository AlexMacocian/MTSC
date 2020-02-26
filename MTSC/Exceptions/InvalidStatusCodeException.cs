using System;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception for invalid response status code.
    /// </summary>
    public sealed class InvalidStatusCodeException : Exception
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
    }
}
