using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception in case of Invalid HTTP Version.
    /// </summary>
    public class InvalidHttpVersionException : Exception
    {
        public InvalidHttpVersionException()
        {
        }

        public InvalidHttpVersionException(string message) : base(message)
        {
        }

        public InvalidHttpVersionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidHttpVersionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
