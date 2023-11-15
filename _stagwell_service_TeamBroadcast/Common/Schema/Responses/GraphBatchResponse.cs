using Newtonsoft.Json;

namespace Breezy.Muticaster.Schema
{   
    public class GraphBatchResponse
    {
        [JsonProperty("responses")]
        public GraphBatchResponseItem[] Responses { get; set; }
    }

    public class GraphBatchResponseItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("body")]
        public dynamic Body { get; set; }
    }
}