using System;
using System.Runtime.Serialization;

namespace MTSC.Exceptions
{
    public class InvalidFtpStatusCodeException : Exception
    {
        public InvalidFtpStatusCodeException()
        {
        }

        public InvalidFtpStatusCodeException(string message) : base(message)
        {
        }

        public InvalidFtpStatusCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidFtpStatusCodeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
