using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public class IncompleteHeaderKeyException : Exception
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

        protected IncompleteHeaderKeyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
