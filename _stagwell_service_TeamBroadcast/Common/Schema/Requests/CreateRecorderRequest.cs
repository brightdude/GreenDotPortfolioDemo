using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Breezy.Muticaster.Schema
{
    public class CreateRecorderRequest
    {
        [OpenApiProperty(Description = "The recorder department ID")]
        [DataType(DataType.Text)]
        [JsonProperty("departmentId")]
        [JsonRequired]
        public string DepartmentId { get; set; }

        [OpenApiProperty(Description = "The recorder display name")]
        [DataType(DataType.Text)]
        [JsonProperty("displayName")]
        [JsonRequired]
        public string DisplayName { get; set; }

        [OpenApiProperty(Description = "The recorder location, this must be unique in the system")]
        [DataType(DataType.Text)]
        [JsonProperty("locationName")]
        [JsonRequired]
        public string LocationName { get; set; }

        [OpenApiProperty(Description = "The recording type ID")]
        [DataType(DataType.Text)]
        [JsonProperty("recordingTypeId")]
        [JsonRequired]
        public string RecordingTypeId { get; set; }

        [OpenApiProperty(Description = "The stream type ID")]
        [DataType(DataType.Text)]
        [JsonProperty("streamTypeId")]
        [JsonRequired]
        public string StreamTypeId { get; set; }
    }
}
