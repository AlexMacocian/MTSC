namespace MTSC.OAuth2.Models
{
    public sealed class AuthorizationOptions
    {
        public enum Authentication
        {
            ClientSecret,
            ClientCertificate
        }

        /// <summary>
        /// Uri of the openid configuration for your OAuth provider.
        /// </summary>
        /// <remarks>
        /// Azure: "https://login.microsoftonline.com/common/.well-known/openid-configuration".
        /// Tenant Azure: "https://login.microsoftonline.com/[TENANT]/.well-known/openid-configuration".
        /// Google: "https://accounts.google.com/.well-known/openid-configuration".
        /// Facebook: "https://www.facebook.com/.well-known/openid-configuration/"
        /// </remarks>
        public string OpenIdConfigurationUri { get; set; }
        /// <summary>
        /// OAuth uri for the OAuth provider.
        /// </summary>
        /// <remarks>
        /// Azure: "https://login.microsoftonline.com/common/oauth2/v2.0".
        /// Azure Tenant: "https://login.microsoftonline.com/{tenant}/oauth2/v2.0".
        /// Google: "https://accounts.google.com/o/oauth2/v2/auth".
        /// Facebook: "https://graph.facebook.com/oauth"
        /// </remarks>
        public string OAuthUri { get; set; }
        /// <summary>
        /// The endpoint for the access token. Most providers use /token but some providers use /access_token or other endpoints.
        /// If your provider uses a different endpoint than /token, override this property with the correct endpoint.
        /// </summary>
        public string AuthTokenEndpoint { get; set; } = "token";
        /// <summary>
        /// ClientID of your Application, as it is registered on your OAuth provider.
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Thumbprint of your client certificate that is whitelisted as your Application on your OAuth provider.
        /// </summary>
        public string ClientCertificateThumbprint { get; set; }
        /// <summary>
        /// Value provided by your OAuth provider to authenticate your service as your Application.
        /// </summary>
        public string ClientSecret { get; set; }
        /// <summary>
        /// URI to redirect the request once authorization succeeds. This URI needs to be whitelisted with your
        /// OAuth provider.
        /// </summary>
        public string RedirectUri { get; set; }
        /// <summary>
        /// Scopes of your authorization request. Example "User.Read" to read the user information.
        /// </summary>
        public string Scopes { get; set; }
        /// <summary>
        /// Specify what to use for authentication.
        /// When using <see cref="Authentication.ClientSecret"/>, the authorization flow will use the <see cref="ClientSecret"/> to authenticate with the OAuth provider.
        /// When using <see cref="Authentication.ClientCertificate"/>, the authorization flow will use the <see cref="ClientCertificateThumbprint"/> to authenticate with the OAuth provider.
        /// </summary>
        public Authentication AuthenticationMode { get; set; }
    }
}
