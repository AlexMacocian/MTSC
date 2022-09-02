using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MTSC.OAuth2.Models
{
    internal sealed class OpenIdConfiguration
    {
        [JsonProperty("issuer")]
        public string Issuer { get; set; }
        [JsonProperty("id_token_signing_alg_values_supported")]
        public List<string> SupportedAlgorithms { get; set; }
        [JsonProperty("claims_supported")]
        public List<string> SupportedClaims { get; set; }
        [JsonProperty("jwks_uri")]
        public string SigningKeysUri { get; set; }
        public List<JsonWebKey> SigningKeys { get; set; }
    }
}
