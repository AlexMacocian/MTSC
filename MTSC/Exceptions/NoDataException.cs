using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MTSC.Exceptions
{
    public class NoDataException : Exception
    {
        public NoDataException()
        {
        }

        public NoDataException(string message) : base(message)
        {
        }

        public NoDataException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NoDataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
