using System;

namespace MTSC.Exceptions
{
    public sealed class IncompleteHttpVersionException : Exception
    {
        public IncompleteHttpVersionException()
        {
        }

        public IncompleteHttpVersionException(string message) : base(message)
        {
        }

        public IncompleteHttpVersionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
