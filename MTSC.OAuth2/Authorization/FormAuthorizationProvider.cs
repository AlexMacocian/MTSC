using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MTSC.OAuth2.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Extensions;
using System.Net.Http;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Authorization
{
    internal abstract class FormAuthorizationProvider<T> : IAuthorizationProvider
    {
        private const string RedirectUri = "{SERVERPLACEHOLDER}/authorize?response_type=code&client_id={CLIENTID}&redirect_uri={REDIRECTURI}&scope={SCOPE}&state={STATE}";
        private const string PostAuthCodeUri = "{SERVERPLACEHOLDER}/{TOKEN}";
        private const string ClientIdPlaceholder = "{CLIENTID}";
        private const string RedirectUriPlaceholder = "{REDIRECTURI}";
        private const string ScopePlaceholder = "{SCOPE}";
        private const string StatePlaceholder = "{STATE}";
        private const string ServerPlaceholder = "{SERVERPLACEHOLDER}";
        private const string TokenPlaceholder = "{TOKEN}";

        private readonly AccessTokenValidator<T> accessTokenValidator;

        protected AuthorizationHttpClientWrapper<T> AuthorizationClientWrapper { get; set; }
        protected AuthorizationOptions Options { get; set; }

        public FormAuthorizationProvider(
            IHttpClient<T> httpClient,
            AuthorizationOptions options)
        {
            this.AuthorizationClientWrapper = new AuthorizationHttpClientWrapper<T>(httpClient);
            this.Options = options ?? throw new ArgumentNullException(nameof(options));
            this.accessTokenValidator = new AccessTokenValidator<T>(this.AuthorizationClientWrapper, this.Options.ClientId, this.Options.OpenIdConfigurationUri);
        }

        public FormAuthorizationProvider(
            HttpClient httpClient,
            AuthorizationOptions options)
        {
            this.AuthorizationClientWrapper = new AuthorizationHttpClientWrapper<T>(httpClient);
            this.Options = options ?? throw new ArgumentNullException(nameof(options));
            this.accessTokenValidator = new AccessTokenValidator<T>(this.AuthorizationClientWrapper, this.Options.ClientId, this.Options.OpenIdConfigurationUri);
        }

        public async Task<TokenValidationResponse> VerifyAccessToken(string accessToken)
        {
            var validationResult = await this.accessTokenValidator.ValidateAccessToken(accessToken);
            return validationResult.Switch<TokenValidationResponse>(
                onSuccess: result => result,
                onFailure: exception =>
                {
                    return new TokenValidationResponse { IsValid = false };
                });
        }

        public Task<string> GetOAuthUri(string state)
        {
            return Task.FromResult(RedirectUri
                .Replace(ServerPlaceholder, this.Options.OAuthUri)
                .Replace(RedirectUriPlaceholder, this.Options.RedirectUri)
                .Replace(ClientIdPlaceholder, this.Options.ClientId)
                .Replace(ScopePlaceholder, this.Options.Scopes)
                .Replace(StatePlaceholder, state));
        }

        public async Task<Optional<JsonWebToken>> RetrieveAccessToken(
            string authorizationCode)
        {
            using var formContent = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", authorizationCode },
                    { "redirect_uri", this.Options.RedirectUri },
                    { "client_id", this.Options.ClientId },
                }.AddRange(GetAdditionalFormFields()));

            var response = await this.AuthorizationClientWrapper.PostAsync(
                PostAuthCodeUri
                .Replace(ServerPlaceholder, this.Options.OAuthUri)
                .Replace(TokenPlaceholder, this.Options.AuthTokenEndpoint), formContent);
            if (response.IsSuccessStatusCode is false)
            {
                return Optional.None<JsonWebToken>();
            }

            var accessToken = JsonConvert.DeserializeObject<AccessTokenResponse>(await response.Content.ReadAsStringAsync());
            var jsonWebTokenHandler = new JsonWebTokenHandler();
            var valid = await jsonWebTokenHandler.ValidateTokenAsync(accessToken.AccessToken, new TokenValidationParameters());
            var token = jsonWebTokenHandler.ReadJsonWebToken(accessToken.AccessToken);
            return token;
        }

        public Task<string> GetRedirectUri()
        {
            return Task.FromResult(this.Options.RedirectUri);
        }

        protected virtual IEnumerable<KeyValuePair<string, string>> GetAdditionalFormFields() { return Array.Empty<KeyValuePair<string, string>>(); }
    }
}
