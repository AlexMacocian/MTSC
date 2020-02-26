using System;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception in case of Invalid HTTP Version.
    /// </summary>
    public sealed class InvalidHttpVersionException : Exception
    {
        public InvalidHttpVersionException()
        {
        }

        public InvalidHttpVersionException(string message) : base(message)
        {
        }

        public InvalidHttpVersionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
