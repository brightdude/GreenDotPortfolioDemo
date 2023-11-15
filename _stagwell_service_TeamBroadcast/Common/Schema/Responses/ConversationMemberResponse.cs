using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Breezy.Muticaster.Schema
{
    public class EventConversationMembersResponse
    {
        [OpenApiProperty(Description = "Event ID")]
        [DataType(DataType.Text)]
        [JsonProperty("eventId")]
        [JsonRequired]
        public string EventId { get; set; }

        [OpenApiProperty(Description = "Members")]     
        [JsonProperty("members")]
        public ConversationMember[] Members { get; set; } = Array.Empty<ConversationMember>();
    }

    public class MeetingConversationMembersResponse
    {
        [OpenApiProperty(Description = "External calendar ID")]
        [DataType(DataType.Text)]
        [JsonProperty("externalCalendarId")]
        [JsonRequired]
        public string ExternalCalendarId { get; set; }

        [OpenApiProperty(Description = "Microsoft meeting ID")]
        [DataType(DataType.Text)]
        [JsonProperty("msMeetingId")]
        [JsonRequired]
        public string MsMeetingId { get; set; }

        [OpenApiProperty(Description = "Members")]        
        [JsonProperty("members")]      
        public ConversationMember[] Members { get; set; } = Array.Empty<ConversationMember>();
    }

    public class ConversationMember
    {
        [OpenApiProperty(Description = "Member id")]
        [DataType(DataType.Text)]
        [JsonProperty("id")]
        [JsonRequired]
        public string Id { get; set; }

        [OpenApiProperty(Description = "Display name")]
        [DataType(DataType.Text)]
        [JsonProperty("displayName")]
        [JsonRequired]
        public string DisplayName { get; set; }

        [OpenApiProperty(Description = "Visible history start")]
        [DataType(DataType.DateTime)]
        [JsonProperty("visibleHistoryStartDateTime", NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? VisibleHistoryStartDateTime { get; set; }
    }

    internal static class ConversationMemberExtensions
    {
        public static ConversationMember ToDto(this Microsoft.Graph.ConversationMember member)
        {
            return new ConversationMember
            {
                Id = member.Id,
                DisplayName = member.DisplayName,
                VisibleHistoryStartDateTime = member.VisibleHistoryStartDateTime
            };
        }
    }
}
