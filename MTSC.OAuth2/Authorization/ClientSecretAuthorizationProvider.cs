using MTSC.OAuth2.Models;
using Slim.Attributes;
using System.Collections.Generic;
using System.Net.Http;

namespace MTSC.OAuth2.Authorization
{
    internal sealed class ClientSecretAuthorizationProvider : FormAuthorizationProvider<ClientSecretAuthorizationProvider>
    {
        [PreferredConstructor(Priority = 0)]
        public ClientSecretAuthorizationProvider(
            AuthorizationOptions options,
            IHttpClient<ClientSecretAuthorizationProvider> httpClient)
            : base(httpClient, options)
        {
        }

        [PreferredConstructor(Priority = 1)]
        public ClientSecretAuthorizationProvider(
            AuthorizationOptions options,
            HttpClient httpClient)
            : base(httpClient, options)
        {
        }

        [PreferredConstructor(Priority = 2)]
        public ClientSecretAuthorizationProvider(
            AuthorizationOptions options)
            : base(new HttpClient(), options)
        {
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetAdditionalFormFields()
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("client_secret", this.Options.ClientSecret)
            };
        }
    }
}
