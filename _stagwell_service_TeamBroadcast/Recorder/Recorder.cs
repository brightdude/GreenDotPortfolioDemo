using Ardalis.GuardClauses;
using Breezy.Muticaster.TenantSettings;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Breezy.Muticaster
{
    public partial class Recorder
    {
        private readonly IOptionsMonitor<MsTeamsOptions> _options;
        private readonly IBreezyCosmosService _breezyCosmosService;
        private readonly IRecorderService _recorderService;
        private readonly IGraphTeamsMembersService _graphTeamsMembersService;
        private readonly IGraphTeamsChannelsService _graphTeamsChannelsService;
        private readonly IGraphTeamsChannelsMembersService _graphTeamsChannelsMembersService;
        private readonly IGraphUsersService _graphUsersService;
        private readonly IGraphDomainsService _graphDomainsService;
        private readonly TenantSettingsService _tenantSettingsService; //TODO: This need refactoring   
        private readonly IAuthorisationService _authService;

        //TODO: Move data out off function class
        public Recorder()
        {
        }

        [ActivatorUtilitiesConstructor]
        public Recorder(
            IOptionsMonitor<MsTeamsOptions> options,
            IBreezyCosmosService breezyCosmosService,
            IRecorderService recorderService,
            IGraphTeamsMembersService graphTeamsMembersService,
            IGraphTeamsChannelsService graphTeamsChannelsService,
            IGraphTeamsChannelsMembersService graphTeamsChannelsMembersService,
            IGraphUsersService graphUsersService,
            IGraphDomainsService graphDomainsService,
            IAuthorisationService authService)
        {
            _options = Guard.Against.Null(options, nameof(options));
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _recorderService = Guard.Against.Null(recorderService, nameof(recorderService));
            _graphTeamsMembersService = Guard.Against.Null(graphTeamsMembersService, nameof(graphTeamsMembersService));
            _graphTeamsChannelsService = Guard.Against.Null(graphTeamsChannelsService, nameof(graphTeamsChannelsService));
            _graphTeamsChannelsMembersService = Guard.Against.Null(graphTeamsChannelsMembersService, nameof(graphTeamsChannelsMembersService));
            _graphUsersService = Guard.Against.Null(graphUsersService, nameof(graphUsersService));
            _graphDomainsService = Guard.Against.Null(graphDomainsService, nameof(graphDomainsService));
            _tenantSettingsService = new TenantSettingsService(breezyCosmosService);
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        [OpenApiProperty(Description = "The recorder's unique id")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("id")]
        [JsonRequired]
        public string Id { get; set; }
        [OpenApiProperty(Description = "The recorder's access level")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("accessLevel")]
        public string AccessLevel { get; set; }
        [OpenApiProperty(Description = "Flags whether the recorder is active")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("activeFlag")]
        public bool ActiveFlag { get; set; }
        [OpenApiProperty(Description = "The id of the department to which this recorder belongs")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("departmentId")]
        [JsonRequired]
        public string DepartmentId { get; set; }
        [OpenApiProperty(Description = "The name of the department to which this recorder belongs")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("departmentName")]
        public string DepartmentName { get; set; }
        [OpenApiProperty(Description = "The name as it should be displayed")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("displayName")]
        [JsonRequired]
        public string DisplayName { get; set; }
        [OpenApiProperty(Description = "The recorder's email address. This must be unique in the system.")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        [JsonProperty("email")]
        [JsonRequired]
        public string Email { get; set; }
        [OpenApiProperty(Description = "The recorder's location")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("locationName")]
        [JsonRequired]
        public string LocationName { get; set; }
        [OpenApiProperty(Description = "The recorder's Active Directory id")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("msAadId")]
        public string MsAadId { get; set; }
        [OpenApiProperty(Description = "The recorder's provisioning status")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("provisioningStatus")]
        public string ProvisioningStatus { get; set; }
        [OpenApiProperty(Description = "The id of the recording type")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("recordingTypeId")]
        [JsonRequired]
        public string RecordingTypeId { get; set; }
        [OpenApiProperty(Description = "The name of the recording type")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("recordingTypeName")]
        public string RecordingTypeName { get; set; }
        [OpenApiProperty(Description = "The id of the stream type")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("streamTypeId")]
        public string StreamTypeId { get; set; }
        [OpenApiProperty(Description = "The name of the stream type")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("streamTypeName")]
        public string StreamTypeName { get; set; }
        [OpenApiProperty(Description = "The ids of any calendars associated with this recorder")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("calendars")]
        public string[] Calendars { get; set; } = Array.Empty<string>();
    }
}
