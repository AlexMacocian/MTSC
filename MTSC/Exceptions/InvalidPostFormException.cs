using System;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception for invalid post forms.
    /// </summary>
    public sealed class InvalidPostFormException : Exception
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
    }
}
