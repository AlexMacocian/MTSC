using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public class IncompleteHttpVersionException : Exception
    {
        public IncompleteHttpVersionException()
        {
        }

        public IncompleteHttpVersionException(string message) : base(message)
        {
        }

        public IncompleteHttpVersionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IncompleteHttpVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
