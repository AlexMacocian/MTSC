using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception for invalid post forms.
    /// </summary>
    public class InvalidPostFormException : Exception
    {
        public InvalidPostFormException()
        {
        }

        public InvalidPostFormException(string message) : base(message)
        {
        }

        public InvalidPostFormException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InvalidPostFormException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
