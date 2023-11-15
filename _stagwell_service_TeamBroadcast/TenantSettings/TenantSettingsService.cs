using Ardalis.GuardClauses;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Breezy.Muticaster.TenantSettings
{
    //TODO: Need refactoring
    public class TenantSettingsService
    {
        private readonly IBreezyCosmosService _breezyCosmosService;

        public TenantSettingsService(IBreezyCosmosService breezyCosmosService)
        {
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
        }

        private TenantSettings Settings { get; set; }

        public async Task<TenantSettings> GetTenantSettings()
        {
            if (Settings == null)
            {
                Settings = await _breezyCosmosService.GetItem<TenantSettings>(breezyContainers.Settings, "TenantSettings", "TenantSettings");
            }
            return Settings;
        }
    }

    public partial class TenantSettings
    {
        [OpenApiProperty(Description = "The unique id of the settings")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("id")]
        public string Id { get; set; }
        [OpenApiProperty(Description = "The tenant id")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("tenantId")]
        public string TenantId { get; set; }
        [OpenApiProperty(Description = "The default usage location (for licensing purposes)")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("defaultUsageLocation")]
        public string DefaultUsageLocation { get; set; }
        [OpenApiProperty(Description = "The list of default channels")]
        [JsonProperty("channels")]
        public ChannelDescription[] Channels { get; set; }
        [OpenApiProperty(Description = "The list of meetings channel apps")]
        [JsonProperty("meetingsChannelApps")]
        public MeetingChannelDescription[] MeetingsChannelApps { get; set; }
        [OpenApiProperty(Description = "The list of access levels")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("accessLevelDescriptions")]
        public AccessLevelDescription[] AccessLevelDescriptions { get; set; }
        [OpenApiProperty(Description = "The list of valid recorder provisioning statuses")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("recorderProvisioningStatusValues")]
        public string[] RecorderProvisioningStatusValues { get; set; }
        [OpenApiProperty(Description = "The list of valid recording types")]
        [JsonProperty("recordingTypeValues")]
        public LookupItem[] RecordingTypeValues { get; set; }
        [OpenApiProperty(Description = "The list of valid stream types")]
        [JsonProperty("streamTypeValues")]
        public LookupItem[] StreamTypeValues { get; set; }
    }

    public class ChannelDescription
    {
        [OpenApiProperty(Description = "The name of the channel")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("name")]
        public string Name { get; set; }
        [OpenApiProperty(Description = "Whether this is a default channel")]
        [JsonProperty("isDefaultChannel")]
        public bool IsDefaultChannel { get; set; }
        [OpenApiProperty(Description = "Whether to add a recorder user")]
        [JsonProperty("addRecorderUser")]
        public bool AddRecorderUser { get; set; }
        [OpenApiProperty(Description = "The team type for this channel")]
        [JsonProperty("type")]
        public TeamType Type { get; set; }
        [OpenApiProperty(Description = "The membership type for this channel")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("membershipType")]
        public string MembershipType { get; set; }
        [OpenApiProperty(Description = "The channels access level(s)")]
        [JsonProperty("accessLevels")]
        public string[] AccessLevels { get; set; }
    }

    public class MeetingChannelDescription
    {
        [OpenApiProperty(Description = "The unique id of the MS Teams app")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("teamsAppId")]
        public string TeamsAppId { get; set; }
        [OpenApiProperty(Description = "The display name of the MS Teams app")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        [OpenApiProperty(Description = "The URL of the MS Teams app")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        [JsonProperty("contentUrl")]
        public string ContentUrl { get; set; }
    }

    public class AccessLevelDescription
    {
        [OpenApiProperty(Description = "The name of the access level")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("accessLevel")]
        public string AccessLevel { get; set; }
        [OpenApiProperty(Description = "The description of the access level")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("description")]
        public string Description { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TeamType
    {
        [EnumMember(Value = "party")]
        Party,
        [EnumMember(Value = "facility")]
        Facility
    }
}
