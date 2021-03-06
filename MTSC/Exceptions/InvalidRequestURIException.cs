﻿using System;

namespace MTSC.Exceptions
{
    /// <summary>
    /// Exception cause by an invalid request URI.
    /// </summary>
    public sealed class InvalidRequestURIException : Exception
    {
        public InvalidRequestURIException()
        {
        }

        public InvalidRequestURIException(string message) : base(message)
        {
        }

        public InvalidRequestURIException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
