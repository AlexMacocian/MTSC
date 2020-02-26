using System;

namespace MTSC.Exceptions
{
    public sealed class IncompleteRequestBodyException : Exception
    {
        public IncompleteRequestBodyException()
        {
        }

        public IncompleteRequestBodyException(string message) : base(message)
        {
        }

        public IncompleteRequestBodyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
