using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Invalid HTTP Method Exception.
    /// </summary>
    public class MethodInvalidException : Exception
    {
        public MethodInvalidException()
        {
        }

        public MethodInvalidException(string message) : base(message)
        {
        }

        public MethodInvalidException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MethodInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
