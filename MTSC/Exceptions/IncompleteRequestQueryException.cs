using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public class IncompleteRequestQueryException : Exception
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

        protected IncompleteRequestQueryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
