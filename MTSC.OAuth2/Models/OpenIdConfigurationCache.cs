using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTSC.OAuth2.Models
{
    internal sealed class OpenIdConfigurationCache
    {
        private static ConcurrentDictionary<string, OpenIdConfigurationCache> CacheDictionary = new();

        private readonly string openIdConfigurationUri;
        private readonly TimeSpan configurationExpirationTime = TimeSpan.FromMinutes(30);

        private DateTime lastConfigRetrieveTime = DateTime.MinValue;
        private OpenIdConfiguration openIdConfiguration;

        private OpenIdConfigurationCache(string openIdConfigurationUri)
        {
            this.openIdConfigurationUri = openIdConfigurationUri;
        }

        internal async Task<OpenIdConfiguration> GetOpenIdConfiguration<T>(AuthorizationHttpClientWrapper<T> httpClientWrapper)
        {
            if (this.openIdConfiguration is null ||
                DateTime.Now - this.lastConfigRetrieveTime > configurationExpirationTime)
            {
                await this.RetrieveOpenIdConfiguration(httpClientWrapper);
            }

            return this.openIdConfiguration;
        }

        private async Task RetrieveOpenIdConfiguration<T>(AuthorizationHttpClientWrapper<T> httpClientWrapper)
        {
            var response = await httpClientWrapper.GetAsync(openIdConfigurationUri);
            if (response.IsSuccessStatusCode is false)
            {
                throw new InvalidOperationException($"Unable to retrieve openid configuration from {this.openIdConfigurationUri}. Response status code {response.StatusCode}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            this.openIdConfiguration = JsonConvert.DeserializeObject<OpenIdConfiguration>(responseString);

            var keysResponse = await httpClientWrapper.GetAsync(this.openIdConfiguration.SigningKeysUri);
            if (keysResponse.IsSuccessStatusCode is false)
            {
                throw new InvalidOperationException($"Unable to retrieve openid signing keys from {this.openIdConfiguration.SigningKeysUri}. Response status code {response.StatusCode}");
            }

            var keysResponseString = await keysResponse.Content.ReadAsStringAsync();
            this.openIdConfiguration.SigningKeys = new List<JsonWebKey>(JsonConvert.DeserializeObject<SigningKeysResponse>(keysResponseString).SigningKeys.Select(signingKey => new JsonWebKey(JsonConvert.SerializeObject(signingKey))));
            this.lastConfigRetrieveTime = DateTime.Now;
        }

        internal static OpenIdConfigurationCache GetForConfigurationUri(string configurationUri)
        {
            if (CacheDictionary.TryGetValue(configurationUri, out var openIdConfigurationCache))
            {
                return openIdConfigurationCache;
            }

            var newOpenIdConfigurationCache = new OpenIdConfigurationCache(configurationUri);
            CacheDictionary.AddOrUpdate(configurationUri, newOpenIdConfigurationCache, (uri, oldValue) => newOpenIdConfigurationCache);

            return newOpenIdConfigurationCache;
        }
    }
}
