using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    class IncompleteHeaderValueException : Exception
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

        protected IncompleteHeaderValueException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
