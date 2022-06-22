using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace MTSC.OAuth2.Models
{
    internal sealed class SigningKeysResponse
    {
        [JsonProperty("keys")]
        public List<JObject> SigningKeys { get; set; }
    }
}
