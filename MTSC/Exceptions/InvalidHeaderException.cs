using System;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception for invalid headers.
    /// </summary>
    public sealed class InvalidHeaderException : Exception
    {
        public InvalidHeaderException()
        {
        }

        public InvalidHeaderException(string message) : base(message)
        {
        }

        public InvalidHeaderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
