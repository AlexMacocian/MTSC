using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MTSC.OAuth2.Models;
using Slim.Attributes;
using System;
using System.Collections.Generic;
using System.Extensions;
using System.Net.Http;

namespace MTSC.OAuth2.Authorization
{
    internal sealed class ClientCertificateAuthorizationProvider : FormAuthorizationProvider<ClientCertificateAuthorizationProvider>
    {
        private readonly AuthorizationOptions authorizationOptions;

        [PreferredConstructor(Priority = 0)]
        public ClientCertificateAuthorizationProvider(
            IHttpClient<ClientCertificateAuthorizationProvider> httpClient,
            AuthorizationOptions authorizationOptions)
            : base(httpClient, authorizationOptions)
        {
            this.authorizationOptions = authorizationOptions.ThrowIfNull(nameof(authorizationOptions));
            this.authorizationOptions.ClientCertificate.ThrowIfNull(nameof(this.authorizationOptions.ClientCertificate));
        }

        [PreferredConstructor(Priority = 1)]
        public ClientCertificateAuthorizationProvider(
            HttpClient httpClient,
            AuthorizationOptions authorizationOptions)
            : base(httpClient, authorizationOptions)
        {
            this.authorizationOptions = authorizationOptions.ThrowIfNull(nameof(authorizationOptions));
            this.authorizationOptions.ClientCertificate.ThrowIfNull(nameof(this.authorizationOptions.ClientCertificate));
        }

        [PreferredConstructor(Priority = 2)]
        public ClientCertificateAuthorizationProvider(
            AuthorizationOptions authorizationOptions)
            : base(new HttpClient(), authorizationOptions)
        {
            this.authorizationOptions = authorizationOptions.ThrowIfNull(nameof(authorizationOptions));
            this.authorizationOptions.ClientCertificate.ThrowIfNull(nameof(this.authorizationOptions.ClientCertificate));
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetAdditionalFormFields()
        {
            var claims = this.GetClaims();
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = claims,
                SigningCredentials = new X509SigningCredentials(this.authorizationOptions.ClientCertificate)
            };

            var handler = new JsonWebTokenHandler();
            var signedClientAssertion = handler.CreateToken(securityTokenDescriptor);

            return new Dictionary<string, string>
            {
                { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                { "client_assertion", signedClientAssertion }
            };
        }

        private Dictionary<string, object> GetClaims()
        {
            return new Dictionary<string, object>()
            {
                { "aud", $"{this.authorizationOptions.OAuthUri}/{this.authorizationOptions.AuthTokenEndpoint}" },
                { "exp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 30 },
                { "iss", this.authorizationOptions.ClientId },
                { "jti", Guid.NewGuid().ToString() },
                { "nbf", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "sub", this.authorizationOptions.ClientId }
            };
        }
    }
}
