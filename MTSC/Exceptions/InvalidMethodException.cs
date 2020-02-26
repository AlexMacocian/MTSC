using System;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Invalid HTTP Method Exception.
    /// </summary>
    public sealed class InvalidMethodException : Exception
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
    }
}
