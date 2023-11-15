using Newtonsoft.Json;

namespace Breezy.Muticaster
{
    public class ApplicationCredential
    {
        [JsonProperty("audience")]
        public string Audience { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("secret")]
        public string Secret { get; set; }

        [JsonProperty("tenant")]
        public string Tenant { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}