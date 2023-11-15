using Ardalis.GuardClauses;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Breezy.Muticaster
{
    public partial class Event : EventCreateUpdateParams
    {
        private readonly IBreezyCosmosService _breezyCosmosService;
        private readonly IGraphOnlineMeetingsService _graphOnlineMeetingsService;
        private readonly IGraphChatsService _graphChatsService;
        private readonly IOptionsMonitor<CredentialOptions> _credentialOptions;
        private readonly IAuthorisationService _authService;

        //TODO: Move data out off function class
        public Event()
        {
        }

        [ActivatorUtilitiesConstructor]
        public Event(
            IBreezyCosmosService breezyCosmosService,
            IGraphOnlineMeetingsService graphOnlineMeetingsService,
            IGraphChatsService graphChatsService,
            IOptionsMonitor<CredentialOptions> credentialOptions,
            IAuthorisationService authService)
        {
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _graphOnlineMeetingsService = Guard.Against.Null(graphOnlineMeetingsService, nameof(graphOnlineMeetingsService));
            _graphChatsService = Guard.Against.Null(graphChatsService, nameof(graphChatsService));
            _credentialOptions = Guard.Against.Null(credentialOptions, nameof(credentialOptions));
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        [OpenApiProperty(Description = "The unique id of this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public string Id { get; set; }

        [OpenApiProperty(Description = "The unique id of the calendar associated with this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("calendarId", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public string CalendarId { get; set; }

        [OpenApiProperty(Description = "The external id of this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("externalId", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public string ExternalId { get; set; }

        [OpenApiProperty(Description = "The internal id for the facility associated with this event (system-generated and read-only)")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("facilityId", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public string FacilityId { get; set; }

        [OpenApiProperty(Description = "The information for joining an event in Microsoft Teams")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("msJoinInfo", NullValueHandling = NullValueHandling.Ignore)]
        public MsJoinInfo MsJoinInfo { get; set; }

        [OpenApiProperty(Description = "The id of the Microsoft Graph meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("msMeetingId", NullValueHandling = NullValueHandling.Ignore)]
        public string MsMeetingId { get; set; }

        [OpenApiProperty(Description = "The id of the Microsoft Graph thread")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("msThreadId")]
        public string MsThreadId { get; set; }

        [OpenApiProperty(Description = "Indicates if an event is active or deleted")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("status", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public string Status { get; set; }

        [OpenApiProperty(Description = "The Microsoft Graph team id", Nullable = true)]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("msTeamId")]
        public string MsTeamId { get; set; }
    }

    public class EventCreateUpdateParams
    {
        [OpenApiProperty(Description = "Message to be included in body of calendar invitations")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("body", NullValueHandling = NullValueHandling.Ignore)]
        public string Body { get; set; }

        [OpenApiProperty(Description = "The id of the case associated with this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("caseId", NullValueHandling = NullValueHandling.Ignore)]
        public string CaseId { get; set; }

        [OpenApiProperty(Description = "The title of the case associated with this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("caseTitle", NullValueHandling = NullValueHandling.Ignore)]
        public string CaseTitle { get; set; }

        [OpenApiProperty(Description = "The type of the case associated with this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("caseType", NullValueHandling = NullValueHandling.Ignore)]
        public string CaseType { get; set; }

        [OpenApiProperty(Description = "The end time of this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.IsoDateTimeConverter))]
        [JsonProperty("endTime", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public DateTime EndTime { get; set; }

        [OpenApiProperty(Description = "Indicates the type of event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("eventType", NullValueHandling = NullValueHandling.Ignore)]
        public string EventType { get; set; }

        [OpenApiProperty(Description = "The focus's unique id for the calendar associated with this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("externalCalendarId", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public string ExternalCalendarId { get; set; }

        [OpenApiProperty(Description = "The list of optional attendees for this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        [JsonProperty("optionalAttendees", NullValueHandling = NullValueHandling.Ignore)]
        public string[] OptionalAttendees { get; set; }

        [OpenApiProperty(Description = "The url used to register parties for this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        [JsonProperty("partyRegistrationUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string PartyRegistrationUrl { get; set; }

        [OpenApiProperty(Description = "The list of required attendees for this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        [JsonProperty("requiredAttendees", NullValueHandling = NullValueHandling.Ignore)]
        public string[] RequiredAttendees { get; set; }

        [OpenApiProperty(Description = "The start time of this event")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.IsoDateTimeConverter))]
        [JsonProperty("startTime", NullValueHandling = NullValueHandling.Ignore)]
        [JsonRequired]
        public DateTime StartTime { get; set; }

        [OpenApiProperty(Description = "Usually the case name. If empty, the subject of the event will be generated from caseId and caseTitle.")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("subject", NullValueHandling = NullValueHandling.Ignore)]
        public string Subject { get; set; }
    }

    public class MsTeamsMeetingInfo
    {
        public string MsMeetingId { get; set; }
        public string MsThreadId { get; set; }
        public MsJoinInfo MsJoinInfo { get; set; }
    }
}
