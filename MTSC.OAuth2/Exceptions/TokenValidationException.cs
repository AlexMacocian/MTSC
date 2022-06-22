using System;

namespace MTSC.OAuth2.Exceptions
{
    public sealed class TokenValidationException : Exception
    {
        private const string ErrorMessage = "Failed to validate access token. See inner exception for details";

        public TokenValidationException(Exception innerException) : base(ErrorMessage, innerException)
        {
        }
    }
}
