using Ardalis.GuardClauses;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface ICalendarService
    {
        Task<Tuple<int, string>> UserRemoveCalendarAssignment(ILogger logger, string email, Calendar calendar);
    }

    //TODO: Need factoring
    internal class CalendarService: ICalendarService
    {
        private readonly IBreezyCosmosService _breezyCosmosService;        
        private readonly IGraphTeamsMembersService _graphTeamsMembersService;
        private readonly IGraphTeamsChannelsService _graphTeamsChannelsService;
        private readonly IGraphTeamsChannelsMembersService _graphTeamsChannelsMembersService;     

        public CalendarService(
            IBreezyCosmosService breezyCosmosService,           
            IGraphTeamsMembersService graphTeamsMembersService,
            IGraphTeamsChannelsService graphTeamsChannelsService,
            IGraphTeamsChannelsMembersService graphTeamsChannelsMembersService)
        {
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));            
            _graphTeamsMembersService = Guard.Against.Null(graphTeamsMembersService, nameof(graphTeamsMembersService));
            _graphTeamsChannelsService = Guard.Against.Null(graphTeamsChannelsService, nameof(graphTeamsChannelsService));
            _graphTeamsChannelsMembersService = Guard.Against.Null(graphTeamsChannelsMembersService, nameof(graphTeamsChannelsMembersService));
        }

        /// <summary>
        /// Removes a focus user from all team channels
        /// </summary>
        public async Task<Tuple<int, string>> UserRemoveCalendarAssignment(ILogger logger, string email, Calendar calendar)
        {
            // Get facility
            var facility = await _breezyCosmosService.GetFacility(calendar.FacilityId);
            return await UserRemoveCalendarAssignment(logger, email, facility, calendar.Id);
        }

        /// <summary>
        /// Removes a focus user from all team channels
        /// </summary>
        private async Task<Tuple<int, string>> UserRemoveCalendarAssignment(ILogger logger, string email, Facility facility, string calendarId)
        {
            // Get user
            var user = await _breezyCosmosService.GetUser(email);
            if (user == null) return Tuple.Create(404, $"User with email '{email}' could not be found");

            // Find other calendars with same facility id and user
            var queryDef = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(c.focusUsers, @email) AND c.id != @calendarId AND c.facilityId = @facilityId")
                .WithParameter("@email", email)
                .WithParameter("@calendarId", calendarId)
                .WithParameter("@facilityId", facility.Id);
            var calendars = await _breezyCosmosService.GetList<Calendar>(breezyContainers.Calendars, queryDef);

            // Terminate if any calendars found - user needs to remain in team channels
            if (calendars.Any()) return new Tuple<int, string>(204, "Not Removed");

            // Remove user from all private channels associated with this facility
            var deleteCount = 0;
            //var graphHelperPriveleged = new GraphHelper(AuthenticationType.ScheduledEventService, logger);
            //var graphHelperApp = new GraphHelper(AuthenticationType.GraphService, logger);
            foreach (var channel in facility.Team.Channels)
            {
                // Retrieve the channel info
                Microsoft.Graph.Channel msChannel;
                try
                {
                    msChannel = await _graphTeamsChannelsService.TeamChannelGet(AuthenticationType.GraphService, facility.Team.MsTeamId, channel.MsChannelId);
                }
                catch (Microsoft.Graph.ServiceException ex)
                {
                    if (ex.StatusCode == HttpStatusCode.NotFound)
                    {
                        logger.LogWarning("Could not find channel '{MsChannelId}' in team '{MsTeamId}'", channel.MsChannelId, facility.Team.MsTeamId);
                        continue;
                    }
                    else throw;
                }

                if (msChannel.MembershipType == Microsoft.Graph.ChannelMembershipType.Private)
                {
                    try
                    {
                        var memberList = await _graphTeamsChannelsMembersService.TeamChannelMemberList(AuthenticationType.GraphService, facility.Team.MsTeamId, channel.MsChannelId);
                        foreach (var member in memberList)
                        {
                            if (member.Id == user.MsAadId)
                            {
                                await _graphTeamsChannelsMembersService.TeamChannelMemberDelete(AuthenticationType.GraphService, facility.Team.MsTeamId, channel.MsChannelId, member.Id);
                                deleteCount++;
                            }
                        }
                    }
                    catch (Microsoft.Graph.ServiceException ex)
                    {
                        logger.LogError(ex, "Could not remove user '{MsAadId}' from channel '{MsChannelId}' in team '{MsTeamId}'", user.MsAadId, channel.MsChannelId, facility.Team.MsTeamId);
                    }
                }
            }

            logger.LogInformation("Removed user '{email}' from {deleteCount} private channels", email, deleteCount);

            // Remove user from team
            var teamMembers = await _graphTeamsMembersService.TeamMembersList(AuthenticationType.GraphService, facility.Team.MsTeamId, user.MsAadId);
            if (teamMembers.Any())
            {
                await _graphTeamsMembersService.TeamMemberDelete(AuthenticationType.GraphService, facility.Team.MsTeamId, teamMembers.First().Id);
                logger.LogInformation("Removed user '{email}' from team '{MsTeamId}'", email, facility.Team.MsTeamId);
            }

            return new Tuple<int, string>(204, "Removed");
        }
    }
}
