using Ardalis.GuardClauses;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Breezy.Muticaster
{
    public partial class Calendar : CalendarCreateUpdateParams
    {
        private readonly IBreezyCosmosService _breezyCosmosService;
        private readonly ICalendarService _calendarService;
        private readonly IRecorderService _recorderService;
        private readonly IUserService _userService;
        private readonly IGraphUsersService _graphUsersService;
        private readonly IGraphOnlineMeetingsService _graphOnlineMeetingsService;
        private readonly IGraphTeamsMembersService _graphTeamsMembersService;
        private readonly IGraphTeamsChannelsService _graphTeamsChannelsService;
        private readonly IGraphTeamsChannelsMembersService _graphTeamsChannelsMembersService;
        private readonly IAuthorisationService _authService;

        //TODO: Move data out off function class
        public Calendar()
        {
        }

        [ActivatorUtilitiesConstructor]
        public Calendar(
            IBreezyCosmosService breezyCosmosService,
            ICalendarService calendarService,
            IRecorderService recorderService,
            IUserService userService,            
            IGraphUsersService graphUsersService,
            IGraphOnlineMeetingsService graphOnlineMeetingsService,
            IGraphTeamsMembersService graphTeamsMembersService,
            IGraphTeamsChannelsService graphTeamsChannelsService,
            IGraphTeamsChannelsMembersService graphTeamsChannelsMembersService,
            IAuthorisationService authService)
        {
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _calendarService = Guard.Against.Null(calendarService, nameof(calendarService));
            _recorderService = Guard.Against.Null(Guard.Against.Null(recorderService, nameof(recorderService)));
            _userService = Guard.Against.Null(Guard.Against.Null(userService, nameof(userService)));
            _graphUsersService = Guard.Against.Null(graphUsersService, nameof(graphUsersService));
            _graphOnlineMeetingsService = Guard.Against.Null(graphOnlineMeetingsService, nameof(graphOnlineMeetingsService));
            _graphTeamsMembersService = Guard.Against.Null(graphTeamsMembersService, nameof(graphTeamsMembersService));
            _graphTeamsChannelsService = Guard.Against.Null(graphTeamsChannelsService, nameof(graphTeamsChannelsService));
            _graphTeamsChannelsMembersService = Guard.Against.Null(graphTeamsChannelsMembersService, nameof(graphTeamsChannelsMembersService));
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        [OpenApiProperty(Description = "The name of the department to which this calendar belongs")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("departmentName", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public string DepartmentName { get; set; }

        [OpenApiProperty(Description = "The unique id of the calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public string Id { get; set; }

        [OpenApiProperty(Description = "The Microsoft Graph team id", Nullable = true)]
        [JsonProperty("msTeamId")]
        public string MsTeamId { get; set; }

        public CalendarIdentity ToCalendarIdentity()
        {
            return new CalendarIdentity
            {
                Id = Id,
                ExternalCalendarId = ExternalCalendarId
            };
        }
    }

    public class CalendarCreateUpdateParams
    {
        [OpenApiProperty(Description = "The display name of this calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("calendarName", NullValueHandling = NullValueHandling.Ignore)]
        public string CalendarName { get; set; }

        [OpenApiProperty(Description = "The list of the focus's users assigned to this calendar - email only. Roles and other details are assigned in the /users endpoint. Users specified here must be provisioned in the system prior to adding to calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        [JsonProperty("focusUsers", NullValueHandling = NullValueHandling.Ignore)]
        public string[] focusUsers { get; set; }

        [OpenApiProperty(Description = "The id of the department to which this calendar belongs")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("departmentId", NullValueHandling = NullValueHandling.Ignore)]
        public string DepartmentId { get; set; }

        [OpenApiProperty(Description = "The focus's unique id for this calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("externalCalendarId", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public string ExternalCalendarId { get; set; }

        [OpenApiProperty(Description = "The id of the facility in which this calendar's matters will be heard. Facilities are provisioned via the /facilities endpoint. Facility specified here must exist prior to adding to the calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("facilityId", NullValueHandling = NullValueHandling.Ignore)]
        public string FacilityId { get; set; }

        [OpenApiProperty(Description = "The list of holding calls associated with this calendar", Nullable = true)]
        [JsonProperty("holdingCalls", NullValueHandling = NullValueHandling.Ignore)]
        public HoldingCall[] HoldingCalls { get; set; }

        [OpenApiProperty(Description = "The list of the recorders assigned to this calendar - email only. Recorders specified here must be provisioned in the system prior to adding to calendar")]
        [JsonProperty("recorders", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Recorders { get; set; }
    }

    public class CalendarIdentity
    {
        [OpenApiProperty(Description = "The unique id of the calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [Required]
        public string Id { get; set; }

        [OpenApiProperty(Description = "The focus's unique id for this calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("externalCalendarId", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public string ExternalCalendarId { get; set; }
    }

    public class CalendarSummary : CalendarIdentity
    {
        [OpenApiProperty(Description = "The display name of this calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("calendarName", NullValueHandling = NullValueHandling.Ignore)]
        public string CalendarName { get; set; }

        [OpenApiProperty(Description = "The id of the facility in which this calendar's matters will be heard")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("facilityId", NullValueHandling = NullValueHandling.Ignore)]
        public string FacilityId { get; set; }

        [OpenApiProperty(Description = "The name of the facility in which this calendar's matters will be heard")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("facilityName", NullValueHandling = NullValueHandling.Ignore)]
        public string FacilityName { get; set; }

        [OpenApiProperty(Description = "The number of focus users associated with this calendar")]
        [JsonProperty("personnelCount", NullValueHandling = NullValueHandling.Ignore)]
        public int PersonnelCount { get; set; }

        [OpenApiProperty(Description = "The number of recorders associated with this calendar")]
        [JsonProperty("recorderCount", NullValueHandling = NullValueHandling.Ignore)]
        public int RecorderCount { get; set; }
    }
}
