using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Breezy.Muticaster.Schema
{
    public class UpdateRecorderRequest: CreateRecorderRequest
    {
        [OpenApiProperty(Description = "The recorder display name")]
        [DataType(DataType.Text)]
        [JsonProperty("displayName")]       
        public new string DisplayName { get; set; }

        [OpenApiProperty(Description = "The recorder location, this must be unique in the system")]
        [DataType(DataType.Text)]
        [JsonProperty("locationName")]      
        public new string LocationName { get; set; }

        [OpenApiProperty(Description = "The recorder provisioning status")]
        [DataType(DataType.Text)]
        [JsonProperty("provisioningStatus")]       
        public string ProvisioningStatus { get; set; }
    }
}
