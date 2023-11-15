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
    public partial class HoldingCall
    {
        private readonly IBreezyCosmosService _breezyCosmosService;
        private readonly IGraphTeamsService _graphTeamsService;
        private readonly IGraphTeamsChannelsService _graphTeamsChannelsService;
        private readonly IGraphOnlineMeetingsService _graphOnlineMeetingsService;
        private readonly IGraphChatsService _graphChatsService;
        private readonly IOptionsMonitor<CredentialOptions> _credentialOptions;
        private readonly IAuthorisationService _authService;

        //TODO: Move data out off function class
        public HoldingCall()
        {
        }

        [ActivatorUtilitiesConstructor]
        public HoldingCall(
            IBreezyCosmosService breezyCosmosService,
            IGraphTeamsService graphTeamsService,
            IGraphTeamsChannelsService graphTeamsChannelsService,
            IGraphOnlineMeetingsService graphOnlineMeetingsService,
            IGraphChatsService graphChatsService,
            IOptionsMonitor<CredentialOptions> credentialOptions,
            IAuthorisationService authService)
        {
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _graphTeamsService = Guard.Against.Null(graphTeamsService, nameof(graphTeamsService));
            _graphTeamsChannelsService = Guard.Against.Null(graphTeamsChannelsService, nameof(graphTeamsChannelsService));
            _graphOnlineMeetingsService = Guard.Against.Null(graphOnlineMeetingsService, nameof(graphOnlineMeetingsService));
            _graphChatsService = Guard.Against.Null(graphChatsService, nameof(graphChatsService));
            _credentialOptions = Guard.Against.Null(credentialOptions, nameof(credentialOptions));
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        [OpenApiProperty(Description = "The start time of the holding call in UTC")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.IsoDateTimeConverter))]
        [JsonProperty("startTime")]
        [JsonRequired]
        public DateTime StartTime { get; set; }
        [OpenApiProperty(Description = "End end time of the holding call in UTC")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.IsoDateTimeConverter))]
        [JsonProperty("endTime")]
        [JsonRequired]
        public DateTime EndTime { get; set; }
        [OpenApiProperty(Description = "Indicates whether the holding call has expired")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("isExpired")]
        public bool? IsExpired { get; set; }
        [OpenApiProperty(Description = "The id of the Microsoft Graph meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("msMeetingId")]
        public string MsMeetingId { get; set; }
        [OpenApiProperty(Description = "The id of the Microsoft Graph thread")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("msThreadId")]
        public string MsThreadId { get; set; }
        [OpenApiProperty(Description = "The Microsoft Graph meeting join information for this holding call")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Custom)]
        [JsonProperty("msJoinInfo")]
        public MsJoinInfo MSJoinInfo { get; set; }
    }

    public class CalendarHoldingCall
    {
        [OpenApiProperty(Description = "The external id of the calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("externalCalendarId")]
        [JsonRequired]
        public string ExternalCalendarId { get; set; }
        [OpenApiProperty(Description = "The name of the calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("calendarName")]
        [JsonRequired]
        public string CalendarName { get; set; }
        [OpenApiProperty(Description = "The holding call associated with the calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Custom)]
        [JsonProperty("holdingCall")]
        [JsonRequired]
        public HoldingCall HoldingCall { get; set; }
    }

    public class CalendarHoldingCalls
    {
        [OpenApiProperty(Description = "The external id of the calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("externalCalendarId")]
        [JsonRequired]
        public string ExternalCalendarId { get; set; }
        [OpenApiProperty(Description = "The name of the calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("calendarName")]
        [JsonRequired]
        public string CalendarName { get; set; }
        [OpenApiProperty(Description = "The holding calls associated with the calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Custom)]
        [JsonProperty("holdingCalls")]
        [JsonRequired]
        public HoldingCall[] HoldingCalls { get; set; }
    }

    public class MsJoinInfo
    {
        [OpenApiProperty(Description = "The join URL of the online meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        [JsonProperty("url")]
        public string URL { get; set; }
        [OpenApiProperty(Description = "The phone access (dial-in) information for an online meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Custom)]
        [JsonProperty("telephone")]
        public MsTelephoneInfo Telephone { get; set; }

        public static MsJoinInfo FromGraphObject(Microsoft.Graph.OnlineMeeting graphObj)
        {
            if (graphObj == null) return null;

            return new MsJoinInfo()
            {
                URL = graphObj.JoinWebUrl,
                Telephone = MsTelephoneInfo.FromGraphObject(graphObj.AudioConferencing)
            };
        }
    }

    public class MsTelephoneInfo
    {
        [OpenApiProperty(Description = "The conference id of the online meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("conferenceId")]
        public string ConferenceId { get; set; }
        [OpenApiProperty(Description = "The toll number that connects to the Audio Conference Provider")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.PhoneNumber)]
        [JsonProperty("fullNumber")]
        public string FullNumber { get; set; }
        [OpenApiProperty(Description = "A URL to the externally-accessible web page that contains dial-in information")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        [JsonProperty("additionalInformation")]
        public string AdditionalInformation { get; set; }

        public static MsTelephoneInfo FromGraphObject(Microsoft.Graph.AudioConferencing graphObj)
        {
            if (graphObj == null) return null;

            return new MsTelephoneInfo()
            {
                ConferenceId = graphObj.ConferenceId,
                FullNumber = graphObj.TollNumber + ",," + graphObj.ConferenceId + "#",
                AdditionalInformation = graphObj.DialinUrl
            };
        }
    }

    public class HoldingCallCreateParams
    {
        [OpenApiProperty(Description = "The unique external identifier for the calendar")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("externalCalendarId")]
        [JsonRequired]
        public string ExternalCalendarId { get; set; }
        [OpenApiProperty(Description = "The meeting start time in UTC")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.IsoDateTimeConverter))]
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }
        [OpenApiProperty(Description = "The meeting end time in UTC")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.IsoDateTimeConverter))]
        [JsonProperty("endTime")]
        public DateTime EndTime { get; set; }
    }
}