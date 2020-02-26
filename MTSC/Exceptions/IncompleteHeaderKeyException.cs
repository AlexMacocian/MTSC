using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public sealed class IncompleteHeaderKeyException : Exception
    {
        public IncompleteHeaderKeyException()
        {
        }

        public IncompleteHeaderKeyException(string message) : base(message)
        {
        }

        public IncompleteHeaderKeyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
