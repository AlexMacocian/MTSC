using System;

namespace MTSC.Exceptions
{
    public sealed class IncompleteRequestQueryException : Exception
    {
        public IncompleteRequestQueryException()
        {
        }

        public IncompleteRequestQueryException(string message) : base(message)
        {
        }

        public IncompleteRequestQueryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
