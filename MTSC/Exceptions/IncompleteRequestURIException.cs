using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public class IncompleteRequestURIException : Exception
    {
        public IncompleteRequestURIException()
        {
        }

        public IncompleteRequestURIException(string message) : base(message)
        {
        }

        public IncompleteRequestURIException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IncompleteRequestURIException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
