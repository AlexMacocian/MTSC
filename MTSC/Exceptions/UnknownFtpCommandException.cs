using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public class UnknownFtpCommandException : Exception
    {
        public UnknownFtpCommandException()
        {
        }

        public UnknownFtpCommandException(string message) : base(message)
        {
        }

        public UnknownFtpCommandException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownFtpCommandException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
