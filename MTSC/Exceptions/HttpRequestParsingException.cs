using System;

namespace MTSC.Exceptions
{
    public sealed class HttpRequestParsingException : Exception
    {
        public HttpRequestParsingException(string message) : base(message)
        {
        }

        public HttpRequestParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
