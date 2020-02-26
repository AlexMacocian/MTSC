using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public sealed class IncompleteHeaderValueException : Exception
    {
        public IncompleteHeaderValueException()
        {
        }

        public IncompleteHeaderValueException(string message) : base(message)
        {
        }

        public IncompleteHeaderValueException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
