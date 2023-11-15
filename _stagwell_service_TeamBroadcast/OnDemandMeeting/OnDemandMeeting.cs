using Ardalis.GuardClauses;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;

namespace FTR.VirtualJustice
{
    public partial class OnDemandMeeting
    {
        private readonly ICourtConnectCosmosService _courtConnectCosmosService;
        private readonly IGraphTeamsService _graphTeamsService;
        private readonly IGraphChatsService _graphChatsService;
        private readonly IGraphOnlineMeetingsService _graphOnlineMeetingsService;
        private readonly IOptionsMonitor<CredentialOptions> _credentialOptions;
        private readonly IAuthorisationService _authService;

        //TODO: Move data out off function class
        public OnDemandMeeting()
        {
        }

        [ActivatorUtilitiesConstructor]
        public OnDemandMeeting(
            ICourtConnectCosmosService courtConnectCosmosService,
            IGraphTeamsService graphTeamsService,
            IGraphChatsService graphChatsService,
            IGraphOnlineMeetingsService graphOnlineMeetingsService,
            IOptionsMonitor<CredentialOptions> credentialOptions,
            IAuthorisationService authService)
        {
            _courtConnectCosmosService = Guard.Against.Null(courtConnectCosmosService, nameof(courtConnectCosmosService));
            _graphTeamsService = Guard.Against.Null(graphTeamsService, nameof(graphTeamsService));
            _graphChatsService = Guard.Against.Null(graphChatsService, nameof(graphChatsService));
            _graphOnlineMeetingsService = Guard.Against.Null(graphOnlineMeetingsService, nameof(graphOnlineMeetingsService));
            _credentialOptions = Guard.Against.Null(credentialOptions, nameof(credentialOptions));
            _authService = Guard.Against.Null(authService, nameof(authService));
        }

        public static string BuildFacilityName(string regionName, string buildingName, string room)
        {
            return $"{regionName} {buildingName} {room}".Trim();
        }

        [OpenApiProperty(Description = "The unique id of the on-demand meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("id")]
        [JsonRequired]
        public string Id { get; set; }
        [OpenApiProperty(Description = "Flags whether the on-demand meeting is active")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("activeFlag")]
        public bool ActiveFlag { get; set; }
        [OpenApiProperty(Description = "Audio conferencing details for the on-demand meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Custom)]
        [JsonProperty("audioConferencing")]
        public AudioConferencing AudioConferencing { get; set; }
        [OpenApiProperty(Description = "The meeting end time in UTC")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        [JsonProperty("endDateTime")]
        public DateTime EndTime { get; set; }
        [OpenApiProperty(Description = "The id of the facility associated with the on-demand meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("facilityId")]
        [JsonRequired]
        public string FacilityId { get; set; }
        [OpenApiProperty(Description = "The join URL for the on-demand meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        [JsonProperty("joinUrl")]
        public string JoinUrl { get; set; }
        [OpenApiProperty(Description = "The meeting name")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("meetingName")]
        [JsonRequired]
        public string MeetingName { get; set; }
        [OpenApiProperty(Description = "The id of the Microsoft Graph meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("msMeetingId")]
        public string MsMeetingId { get; set; }
        [OpenApiProperty(Description = "The id of the Microsoft Graph team")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("msTeamId")]
        [JsonRequired]
        public string MsTeamId { get; set; }
        [OpenApiProperty(Description = "The id of the Microsoft Graph thread")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("msThreadId")]
        public string MsThreadId { get; set; }
        [OpenApiProperty(Description = "The user principal name of the organizer")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.EmailAddress)]
        [JsonProperty("organizer")]
        public string Organizer { get; set; }
        [OpenApiProperty(Description = "The meeting start time in UTC")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        [JsonProperty("startDateTime")]
        public DateTime StartTime { get; set; }
    }

    public class AudioConferencing
    {
        [OpenApiProperty(Description = "The conference id of the online meeting")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("conferenceId")]
        [JsonRequired]
        public string ConferenceId { get; set; }
        [OpenApiProperty(Description = "A URL to the externally-accessible web page that contains dial-in information")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Url)]
        [JsonProperty("dialInUrl")]
        [JsonRequired]
        public string DialInUrl { get; set; }
        [OpenApiProperty(Description = "List of toll numbers that are displayed in the meeting invite")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.PhoneNumber)]
        [JsonProperty("tollNumbers")]
        public string[] TollNumbers { get; set; }
        [OpenApiProperty(Description = "List of toll-free numbers that are displayed in the meeting invite")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.PhoneNumber)]
        [JsonProperty("tollFreeNumbers")]
        public string[] TollFreeNumbers { get; set; }

        public static AudioConferencing FromGraphObject(Microsoft.Graph.AudioConferencing graphObj)
        {
            if (graphObj == null) return null;

            return new AudioConferencing()
            {
                ConferenceId = graphObj.ConferenceId,
                DialInUrl = graphObj.DialinUrl,
                TollNumbers = graphObj.TollNumbers.ToArray(),
                TollFreeNumbers = graphObj.TollFreeNumbers.ToArray()
            };
        }
    }

    public class OnDemandMeetingCreateParams
    {
        [OpenApiProperty(Description = "The Microsoft Graph teams id")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("teamId")]
        [JsonRequired]
        public string TeamId { get; set; }
        [OpenApiProperty(Description = "The meeting name")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("meetingName")]
        [JsonRequired]
        public string MeetingName { get; set; }
        [OpenApiProperty(Description = "The meeting start time in UTC")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        [JsonProperty("startDateTime")]
        public DateTime? StartTime { get; set; }
        [OpenApiProperty(Description = "The meeting end time in UTC")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.DateTime)]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        [JsonProperty("endDateTime")]
        public DateTime? EndTime { get; set; }
        [OpenApiProperty(Description = "The user principal name of the meeting organizer")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("organizer")]
        public string Organizer { get; set; }
    }

    public class ChatMessageCreateParams
    {
        [OpenApiProperty(Description = "The Microsoft Graph teams id")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("teamId")]
        [JsonRequired]
        public string TeamId { get; set; }
        [OpenApiProperty(Description = "The meeting id")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Text)]
        [JsonProperty("meetingId")]
        [JsonRequired]
        public string MeetingId { get; set; }
        [OpenApiProperty(Description = "The chat message content")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Html)]
        [JsonProperty("content")]
        [JsonRequired]
        public string Content { get; set; }
    }

    public class ChatMessage
    {
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("attachments")]
        public IEnumerable<ChatMessageAttachment> Attachments { get; set; }
        [OpenApiProperty(Description = "Plaintext/HTML representation of the content of the chat message.")]
        [DataType(System.ComponentModel.DataAnnotations.DataType.Custom)]
        [JsonProperty("body")]
        public ChatMessageBody Body { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("channelIdentity")]
        public ChannelIdentity ChannelIdentity { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("chatId")]
        public string ChatId { get; set; }
        [JsonProperty("createdDateTime")]
        public DateTimeOffset? CreatedDateTime { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("deletedDateTime")]
        public DateTimeOffset? DeletedDateTime { get; set; }
        [JsonProperty("etag")]
        public string Etag { get; set; }
        [JsonProperty("from")]
        public ChatMessageFromIdentitySet From { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("importance")]
        [JsonConverter(typeof(StringEnumConverter))]
        [EnumDataType(typeof(OnDemandMeetingMessageImportance))]
        public OnDemandMeetingMessageImportance? Importance { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("lastEditedDateTime")]
        public DateTimeOffset? LastEditedDateTime { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("lastModifiedDateTime")]
        public DateTimeOffset? LastModifiedDateTime { get; set; }
        [JsonProperty("locale")]
        public string Locale { get; set; }
        [JsonProperty("mentions")]
        public IEnumerable<ChatMessageMention> Mentions { get; set; }
        [JsonProperty("messageType")]
        [JsonConverter(typeof(StringEnumConverter))]
        [EnumDataType(typeof(OnDemandMeetingMessageType))]
        public OnDemandMeetingMessageType? MessageType { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("policyViolation")]
        public ChatMessagePolicyViolation PolicyViolation { get; set; }
        [JsonProperty("reactions")]
        public IEnumerable<ChatMessageReaction> Reactions { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("replyToId")]
        public string ReplyToId { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("subject")]
        public string Subject { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("summary")]
        public string Summary { get; set; }
        [OpenApiProperty(Nullable = true)]
        [JsonProperty("webUrl")]
        public string WebUrl { get; set; }

        public static ChatMessage FromGraphObject(Microsoft.Graph.ChatMessage message)
        {
            return new ChatMessage
            {
                Attachments = message.Attachments,
                Body = ChatMessageBody.FromGraphObject(message.Body),
                ChannelIdentity = message.ChannelIdentity,
                ChatId = message.ChatId,
                CreatedDateTime = message.CreatedDateTime,
                DeletedDateTime = message.DeletedDateTime,
                Etag = message.Etag,
                From = message.From,
                Id = message.Id,
                Importance = (OnDemandMeetingMessageImportance)message.Importance,
                LastEditedDateTime = message.LastEditedDateTime,
                LastModifiedDateTime = message.LastModifiedDateTime,
                Locale = message.Locale,
                Mentions = message.Mentions,
                MessageType = (OnDemandMeetingMessageType)message.MessageType,
                PolicyViolation = message.PolicyViolation,
                Reactions = message.Reactions,
                ReplyToId = message.ReplyToId,
                Subject = message.Subject,
                Summary = message.Summary,
                WebUrl = message.WebUrl
            };
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum OnDemandMeetingMessageImportance
    {
        [EnumMember(Value = "normal")]
        Normal,
        [EnumMember(Value = "high")]
        High,
        [EnumMember(Value = "urgent")]
        Urgent,
        [EnumMember(Value = "unknownfuturevalue")]
        UnknownFutureValue
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum OnDemandMeetingMessageType
    {
        [EnumMember(Value = "message")]
        Message,
        [EnumMember(Value = "chatevent")]
        ChatEvent,
        [EnumMember(Value = "typing")]
        Typing,
        [EnumMember(Value = "unknownfuturevalue")]
        UnknownFutureValue
    }

    public class ChatMessageBody
    {
        [OpenApiProperty(Description = "The content of the item")]
        [JsonProperty("content")]
        public string Content { get; set; }

        [OpenApiProperty(Description = "The type of the content. Possible values are text and html.")]
        [JsonProperty("contentType")]
        [JsonConverter(typeof(StringEnumConverter))]
        [EnumDataType(typeof(OnDemandMeetingMessageBodyType))]
        public OnDemandMeetingMessageBodyType? ContentType { get; set; }

        [OpenApiProperty(Description = "Additional data about the message body", Nullable = true)]
        [JsonExtensionData]
        public IDictionary<string, object> AdditionalData { get; set; }

        [OpenApiProperty(Description = "The odata type", Nullable = true)]
        [JsonProperty("@odata.type")]
        public string ODataType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum OnDemandMeetingMessageBodyType
        {
            [EnumMember(Value = "text")]
            Text,
            [EnumMember(Value = "html")]
            Html
        }

        public static ChatMessageBody FromGraphObject(ItemBody body)
        {
            return new ChatMessageBody
            {
                Content = body.Content,
                ContentType = (OnDemandMeetingMessageBodyType)body.ContentType,
                AdditionalData = body.AdditionalData,
                ODataType = body.ODataType
            };
        }
    }
}
