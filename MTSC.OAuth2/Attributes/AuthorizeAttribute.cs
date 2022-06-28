using Microsoft.Extensions.Logging;
using MTSC.Common.Http;
using MTSC.Common.Http.Attributes;
using MTSC.OAuth2.Models;
using System;
using System.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Slim.Attributes;
using MTSC.ServerSide;
using MTSC.OAuth2.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MTSC.OAuth2.Attributes
{
    public sealed class AuthorizeAttribute : RouteFilterAttribute
    {
        private const string StateKey = "state";
        private const string AuthorizationCodeKey = "code";
        private const string MaxAgeAttribute = "Max-Age";
        private const string AccessTokenObject = "AccessTokenObject";
        
        public const string AccessTokenKey = "AccessToken";

        private static readonly RandomNumberGenerator RandomNumberGenerator = RandomNumberGenerator.Create();

        private readonly ILogger<AuthorizeAttribute> logger;
        private readonly IAuthorizationProvider authorizationProvider;

        [DoNotInject]
        ///<summary>
        /// Public parameterless constructor to be used for decorating endpoints. Do not use to instantiate new <see cref="AuthorizeAttribute"/>.
        /// Use instead any of the other constructors.
        ///</summary>
        public AuthorizeAttribute()
        {
        }

        [PreferredConstructor(Priority = 0)]
        public AuthorizeAttribute(
            IAuthorizationProvider authorizationProvider,
            ILogger<AuthorizeAttribute> logger)
        {
            this.authorizationProvider = authorizationProvider.ThrowIfNull(nameof(authorizationProvider));
            this.logger = logger.ThrowIfNull(nameof(logger));
        }

        [PreferredConstructor(Priority = 1)]
        public AuthorizeAttribute(
            IAuthorizationProvider authorizationProvider,
            Server server)
        {
            this.authorizationProvider = authorizationProvider.ThrowIfNull(nameof(authorizationProvider));
            this.logger = new ServerDebugLogger<AuthorizeAttribute>(server.ThrowIfNull(nameof(server)));
        }

        public override async Task<RouteEnablerAsyncResponse> HandleRequestAsync(RouteContext routeContext)
        {
            this.logger.LogInformation("Authorizing request");            
            if (TryGetCookieValue(routeContext.HttpRequest, AccessTokenKey, out var accessToken))
            {
                this.logger.LogInformation("Found authorization token. Validating");
                if(await this.authorizationProvider.VerifyAccessToken(accessToken) is false)
                {
                    this.logger.LogInformation("Validation failed");
                    return await this.ExpireAccessTokenAndRedirect(accessToken);
                }

                this.logger.LogInformation("Validation succeeded");
                routeContext.Resources[AccessTokenKey] = accessToken;
                return RouteEnablerAsyncResponse.Accept;
            }

            if (TryGetCookieValue(routeContext.HttpRequest, StateKey, out var state) &&
                TryParseRequestQuery(routeContext.HttpRequest.RequestQuery, out var queryDictionary))
            {
                this.logger.LogInformation("Received redirect from OAuth server. Checking state value");
                if (queryDictionary.TryGetValue(StateKey, out var redirectState) is false)
                {
                    this.logger.LogInformation("No state value found in query. Rejecting request");
                    return RouteEnablerAsyncResponse.Error(Unauthorized401);
                }

                if (state != redirectState)
                {
                    this.logger.LogInformation("State values do not match. Rejecting request");
                    return RouteEnablerAsyncResponse.Error(Unauthorized401);
                }

                if (queryDictionary.TryGetValue(AuthorizationCodeKey, out var authCode) is false)
                {
                    this.logger.LogInformation("No authorization code found in the query. Rejecting request");
                    return RouteEnablerAsyncResponse.Error(Unauthorized401);
                }

                this.logger.LogInformation("Requesting access token");
                var maybeAccessToken = await this.authorizationProvider.RetrieveAccessToken(authCode);
                var retrievedAccessToken = maybeAccessToken.ExtractValue();
                if (retrievedAccessToken is null)
                {
                    this.logger.LogInformation("Failed to retrieve access token. Rejecting request");
                    return RouteEnablerAsyncResponse.Error(Unauthorized401);
                }

                routeContext.Resources[AccessTokenKey] = retrievedAccessToken.EncodedToken;
                routeContext.Resources[AccessTokenObject] = retrievedAccessToken;
                return await this.RedirectToUri(retrievedAccessToken);
            }

            return await this.RedirectWithStateToOAuth();
        }
        public override void HandleResponse(RouteContext routeContext)
        {
            SetAccessTokenCookieInContext(routeContext);
        }

        private async Task<RouteEnablerAsyncResponse> RedirectWithStateToOAuth()
        {
            var state = GetRandomState();
            this.logger.LogDebug($"Redirecting client to OAuth server with state {state}");
            return RouteEnablerAsyncResponse.Error(await this.OAuthRedirect307(state));
        }
        private async Task<RouteEnablerAsyncResponse> RedirectToUri(JsonWebToken accessTokenObject)
        {
            this.logger.LogDebug($"Redirecting to redirect uri");
            return RouteEnablerAsyncResponse.Error(await this.RedirectToUri307(accessTokenObject));
        }
        private async Task<RouteEnablerAsyncResponse> ExpireAccessTokenAndRedirect(string accessToken)
        {
            this.logger.LogDebug($"Expiring access token and redirecting");
            return RouteEnablerAsyncResponse.Error(await this.ExpireAccessAndRedirect307(accessToken));
        }
        private async Task<HttpResponse> OAuthRedirect307(string state)
        {
            var response = new HttpResponse
            {
                StatusCode = HttpMessage.StatusCodes.TemporaryRedirect
            };

            var oAuthUri = await this.authorizationProvider.GetOAuthUri(state);
            response.Headers.AddHeader(HttpMessage.ResponseHeaders.Location, oAuthUri);
            response.Cookies.Add(new Cookie(StateKey, state));
            return response;
        }
        private async Task<HttpResponse> RedirectToUri307(JsonWebToken accessTokenObject)
        {
            var response = new HttpResponse
            {
                StatusCode = HttpMessage.StatusCodes.TemporaryRedirect
            };

            response.Headers.AddHeader(HttpMessage.ResponseHeaders.Location, await this.authorizationProvider.GetRedirectUri());
            SetAccessTokenCookieInHttpResponse(response, accessTokenObject);
            return response;
        }
        private async Task<HttpResponse> ExpireAccessAndRedirect307(string accessToken)
        {
            var response = new HttpResponse
            {
                StatusCode = HttpMessage.StatusCodes.TemporaryRedirect
            };

            response.Headers.AddHeader(HttpMessage.ResponseHeaders.Location, await this.authorizationProvider.GetRedirectUri());
            ResetAccessTokenCookieInHttpResponse(response, accessToken);
            return response;
        }

        private static void SetAccessTokenCookieInContext(RouteContext routeContext)
        {
            if (routeContext.Resources.TryGetValue(AccessTokenObject, out var accessToken) is false ||
                accessToken is not JsonWebToken accessTokenObject)
            {
                return;
            }

            SetAccessTokenCookieInHttpResponse(routeContext.HttpResponse, accessTokenObject);
        }
        private static void SetAccessTokenCookieInHttpResponse(HttpResponse response, JsonWebToken jsonWebToken)
        {
            var cookie = new Cookie(AccessTokenKey, jsonWebToken.EncodedToken);
            cookie.Attributes.Add(MaxAgeAttribute, (jsonWebToken.ValidTo - DateTime.Now).TotalSeconds.ToString());
            response?.Cookies?.Add(cookie);
        }
        private static void ResetAccessTokenCookieInHttpResponse(HttpResponse response, string accessToken)
        {
            var cookie = new Cookie(AccessTokenKey, accessToken);
            cookie.Attributes.Add(MaxAgeAttribute, "-1");
            response?.Cookies?.Add(cookie);
        }
        private static bool TryParseRequestQuery(string query, out Dictionary<string, string> queryValues)
        {
            queryValues = null;
            if (string.IsNullOrEmpty(query))
            {
                return false;
            }

            try
            {
                queryValues = query.Split('&')
                    .ToDictionary(c => c.Split('=')[0].ToLowerInvariant(), c => Uri.UnescapeDataString(c.Split('=')[1]));
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }
        private static bool TryGetCookieValue(HttpRequest httpRequest, string cookieKey, out string cookieValue)
        {
            var cookie = httpRequest.Cookies.FirstOrDefault(c => c.Key == cookieKey);
            cookieValue = cookie?.Value;
            if (string.IsNullOrWhiteSpace(cookieValue))
            {
                return false;
            }

            return true;
        }
        private static string GetRandomState()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.GetNonZeroBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("=", "")
                .Replace('+', '-')
                .Replace('/', '_');
        }
        private static HttpResponse Unauthorized401 => new()
        {
            StatusCode = HttpMessage.StatusCodes.Unauthorized,
            BodyString = "Authorization failed"
        };
    }
}
