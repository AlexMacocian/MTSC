using System;

namespace MTSC.Exceptions
{
    public sealed class IncompleteRequestException : Exception
    {
        public IncompleteRequestException()
        {
        }

        public IncompleteRequestException(string message) : base(message)
        {
        }

        public IncompleteRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
