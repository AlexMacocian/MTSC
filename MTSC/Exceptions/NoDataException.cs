using System;

namespace MTSC.Exceptions
{
    public sealed class NoDataException : Exception
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
    }
}
