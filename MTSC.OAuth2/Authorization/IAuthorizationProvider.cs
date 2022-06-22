using Microsoft.IdentityModel.JsonWebTokens;
using System.Extensions;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Authorization
{
    public interface IAuthorizationProvider
    {
        Task<Optional<JsonWebToken>> RetrieveAccessToken(string authorizationCode);
        Task<bool> VerifyAccessToken(string accessToken);
        Task<string> GetOAuthUri(string state);
        Task<string> GetRedirectUri();
    }
}
