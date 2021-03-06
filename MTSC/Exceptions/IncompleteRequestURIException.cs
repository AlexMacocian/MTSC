﻿using System;

namespace MTSC.Exceptions
{
    public sealed class IncompleteRequestURIException : Exception
    {
        public IncompleteRequestURIException()
        {
        }

        public IncompleteRequestURIException(string message) : base(message)
        {
        }

        public IncompleteRequestURIException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
