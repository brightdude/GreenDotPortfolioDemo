using Ardalis.GuardClauses;
using Breezy.Muticaster.TenantSettings;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Breezy.Muticaster
{
    public partial class Facility : FacilityCreateUpdateParams
    {
        private readonly IBreezyCosmosService _breezyCosmosService;
        private readonly IGraphTeamsService _graphTeamsService;
        private readonly IGraphTeamsChannelsService _graphTeamsChannelsService;
        private readonly IGraphOnlineMeetingsService _graphOnlineMeetingsService;
        private readonly TenantSettingsService _tenantSettingsService; //TODO: need to refactor
        private readonly IAuthorisationService _authService;

        //TODO: Move data out off function class
        public Facility()
        {
        }

        [ActivatorUtilitiesConstructor]
        public Facility(
            IBreezyCosmosService breezyCosmosService,
            IGraphTeamsService graphTeamsService,
            IGraphTeamsChannelsService graphTeamsChannelsService,
            IGraphOnlineMeetingsService graphOnlineMeetingsService,
            IAuthorisationService authService)
        {
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _graphTeamsService = Guard.Against.Null(graphTeamsService, nameof(graphTeamsService));
            _graphTeamsChannelsService = Guard.Against.Null(graphTeamsChannelsService, nameof(graphTeamsChannelsService));
            _graphOnlineMeetingsService = Guard.Against.Null(graphOnlineMeetingsService, nameof(graphOnlineMeetingsService));
            _tenantSettingsService = new TenantSettingsService(breezyCosmosService);
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        public static string BuildFacilityName(string regionName, string buildingName, string room)
        {
            return $"{regionName} {buildingName} {room}".Trim();
        }

        [OpenApiProperty(Description = "The number of active on-demand meetings associated with this facility", Nullable = true)]
        [JsonProperty("activeOnDemandMeetingCount", NullValueHandling = NullValueHandling.Include)]
        public int? ActiveOnDemandMeetingCount { get; set; }

        [OpenApiProperty(Description = "The name of the building this facility is located in", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("buildingName")]
        public string BuildingName { get; set; }

        [OpenApiProperty(Description = "The identifiers of any calendars associated with this facility", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Custom)]
        [JsonProperty("calendars")]
        public CalendarIdentity[] Calendars { get; set; }

        [OpenApiProperty(Description = "The id of the country this facility is located in", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("countryId")]
        public string CountryId { get; set; }

        [OpenApiProperty(Description = "The name of the country this facility is located in", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("countryName")]
        public string CountryName { get; set; }

        [OpenApiProperty(Description = "The id of the region this facility is located in", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("regionId")]
        public string RegionId { get; set; }

        [OpenApiProperty(Description = "The name of the region this facility is located in", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("regionName")]
        public string RegionName { get; set; }

        [OpenApiProperty(Description = "The id of the state this facility is located in", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("stateId")]
        public string StateId { get; set; }

        [OpenApiProperty(Description = "The name of the state this facility is located in", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("stateName")]
        public string StateName { get; set; }

        [OpenApiProperty(Description = "The id of the sub-region this facility is located in", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("subRegionId")]
        public string SubRegionId { get; set; }

        [OpenApiProperty(Description = "The name of the sub-region this facility is located in", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("subRegionName")]
        public string SubRegionName { get; set; }

        [OpenApiProperty(Description = "Details of the Microsoft Graph team associated with this facility", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Custom)]
        [JsonProperty("team")]
        public FacilityTeamDetails Team { get; set; }
    }

    public class FacilityCreateUpdateParams
    {
        [OpenApiProperty(Description = "The internal id for this facility (system-generated and read-only)", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [OpenApiProperty(Description = "The id of the building this facility is located in")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("buildingId")]
        [JsonRequired]
        public string BuildingId { get; set; }

        [OpenApiProperty(Description = "the descriptive name used to identify this facility")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("displayName")]
        [JsonRequired]
        public string DisplayName { get; set; }

        [OpenApiProperty(Description = "The main use of this facility - currently only 'focusroom' is in use")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("facilityType")]
        [JsonConverter(typeof(StringEnumConverter))]
        [EnumDataType(typeof(FacilityType))]
        [JsonRequired]
        public FacilityType FacilityType { get; set; }

        [OpenApiProperty(Description = "The floor of the building this facility is located on")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("floor")]
        public string Floor { get; set; }

        [OpenApiProperty(Description = "The room number of this facility")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("room")]
        public string Room { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FacilityType
    {
        [EnumMember(Value = "focusroom")]
        focusroom,
        [EnumMember(Value = "hearingRoom")]
        HearingRoom,
        [EnumMember(Value = "conferenceRoom")]
        ConferenceRoom,
        [EnumMember(Value = "mediationFacility")]
        MediationFacility
    }
}

