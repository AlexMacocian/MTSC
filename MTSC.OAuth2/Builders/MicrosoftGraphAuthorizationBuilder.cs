using Microsoft.Extensions.Options;
using MTSC.OAuth2.Authorization;
using MTSC.OAuth2.Models;
using MTSC.ServerSide;
using System;
using System.Extensions;
using static MTSC.OAuth2.Models.AuthorizationOptions;

namespace MTSC.OAuth2.Builders
{
    public sealed class MicrosoftGraphAuthorizationBuilder
    {
        private const string AuthTokenEndpoint = "token";
        private const string TenantPlaceholder = "[TENANT]";
        private const string OAuthUri = "https://login.microsoftonline.com/[TENANT]/oauth2/v2.0";
        private const string OpenIdConfigurationUri = "https://login.microsoftonline.com/[TENANT]/.well-known/openid-configuration";

        private readonly Server chainedServer;
        private string Tenant { get; set; }
        private string ClientId { get; set; }
        private string Scopes { get; set; }
        private Authentication AuthenticationMode { get; set; }
        private string ClientSecret { get; set; }
        private string ClientCertificateThumbprint { get; set; }
        private string RedirectUri { get; set; }

        internal MicrosoftGraphAuthorizationBuilder(Server chainedServer)
        {
            this.chainedServer = chainedServer.ThrowIfNull(nameof(chainedServer));
        }

        public MicrosoftGraphAuthorizationBuilder WithTenant(string tenant)
        {
            this.Tenant = tenant.ThrowIfNull(nameof(tenant));
            return this;
        }

        public MicrosoftGraphAuthorizationBuilder WithClientId(string clientId)
        {
            this.ClientId = clientId.ThrowIfNull(nameof(clientId));
            return this;
        }

        public MicrosoftGraphAuthorizationBuilder WithScopes(string scopes)
        {
            this.Scopes = scopes.ThrowIfNull(nameof(scopes));
            return this;
        }

        public MicrosoftGraphAuthorizationBuilder WithRedirectUri(string redirectUri)
        {
            this.RedirectUri = redirectUri.ThrowIfNull(nameof(redirectUri));
            return this;
        }

        public MicrosoftGraphAuthorizationBuilder WithClientSecret(string clientSecret)
        {
            this.ClientSecret = clientSecret.ThrowIfNull(nameof(clientSecret));
            this.AuthenticationMode = Authentication.ClientSecret;
            return this;
        }

        public MicrosoftGraphAuthorizationBuilder WithClientCertificateThumbprint(string clientCertificateThumbprint)
        {
            this.ClientCertificateThumbprint = clientCertificateThumbprint.ThrowIfNull(nameof(clientCertificateThumbprint));
            this.AuthenticationMode = Authentication.ClientCertificate;
            return this;
        }

        public Server Build()
        {
            this.Tenant.ThrowIfNull(nameof(this.Tenant));
            this.ClientId.ThrowIfNull(nameof(this.Tenant));
            this.Scopes.ThrowIfNull(nameof(this.Tenant));
            this.RedirectUri.ThrowIfNull(nameof(this.RedirectUri));
            var oauthUri = OAuthUri.Replace(TenantPlaceholder, this.Tenant);
            var openIdConfigurationUri = OpenIdConfigurationUri.Replace(TenantPlaceholder, this.Tenant);
            var options = new AuthorizationOptions
            {
                ClientId = this.ClientId,
                Scopes = this.Scopes,
                OpenIdConfigurationUri = openIdConfigurationUri,
                OAuthUri = oauthUri,
                RedirectUri = this.RedirectUri,
                AuthTokenEndpoint = AuthTokenEndpoint
            };

            if (this.AuthenticationMode == Authentication.ClientSecret &&
                this.ClientSecret.ThrowIfNull(nameof(this.ClientSecret)) is not null)
            {
                options.ClientSecret = this.ClientSecret;
                options.AuthenticationMode = Authentication.ClientSecret;
                this.chainedServer.ServiceManager.RegisterScoped<IAuthorizationProvider, ClientSecretAuthorizationProvider>();
            }
            else if (this.ClientCertificateThumbprint.ThrowIfNull(nameof(this.ClientCertificateThumbprint)) is not null)
            {
                options.ClientCertificateThumbprint = this.ClientCertificateThumbprint;
                options.AuthenticationMode = Authentication.ClientCertificate;
                this.chainedServer.ServiceManager.RegisterScoped<IAuthorizationProvider, ClientCertificateAuthorizationProvider>();
            }

            this.chainedServer.ServiceManager.RegisterSingleton(sp => options);
            return this.chainedServer;
        }
    }
}
