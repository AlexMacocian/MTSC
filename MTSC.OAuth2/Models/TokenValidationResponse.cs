using Microsoft.IdentityModel.JsonWebTokens;

namespace MTSC.OAuth2.Models
{
    public sealed class TokenValidationResponse
    {
        public bool IsValid { get; set; }
        public JsonWebToken JsonWebToken { get; set; }
    }
}
