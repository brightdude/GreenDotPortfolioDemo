using Newtonsoft.Json;

namespace Breezy.Muticaster.Schema
{
    public class IdNameDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
