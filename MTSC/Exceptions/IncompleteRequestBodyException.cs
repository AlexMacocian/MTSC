using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public class IncompleteRequestBodyException : Exception
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

        protected IncompleteRequestBodyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
