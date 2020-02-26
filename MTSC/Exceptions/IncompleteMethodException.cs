using System;

namespace MTSC.Exceptions
{
    public sealed class IncompleteMethodException : Exception
    {
        public IncompleteMethodException()
        {
        }

        public IncompleteMethodException(string message) : base(message)
        {
        }

        public IncompleteMethodException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
