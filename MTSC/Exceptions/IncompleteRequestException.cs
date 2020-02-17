using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public class IncompleteRequestException : Exception
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

        protected IncompleteRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
