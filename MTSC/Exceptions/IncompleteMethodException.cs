using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public class IncompleteMethodException : Exception
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

        protected IncompleteMethodException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
