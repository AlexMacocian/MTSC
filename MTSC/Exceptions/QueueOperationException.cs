using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception used in operations on queues
    /// </summary>
    public class QueueOperationException : Exception
    {
        public QueueOperationException()
        {
        }

        public QueueOperationException(string message) : base(message)
        {
        }

        public QueueOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected QueueOperationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
