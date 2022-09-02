using Microsoft.IdentityModel.JsonWebTokens;
using MTSC.OAuth2.Models;
using System.Extensions;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Authorization
{
    public interface IAuthorizationProvider
    {
        Task<Optional<JsonWebToken>> RetrieveAccessToken(string authorizationCode);
        Task<TokenValidationResponse> VerifyAccessToken(string accessToken);
        Task<string> GetOAuthUri(string state);
        Task<string> GetRedirectUri();
    }
}
