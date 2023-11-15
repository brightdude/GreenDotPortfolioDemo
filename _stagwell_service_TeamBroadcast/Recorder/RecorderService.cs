using Ardalis.GuardClauses;
using Breezy.Muticaster.TenantSettings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface IRecorderService
    {
        Task RecorderAssignToTeamAndChannels(ILogger logger, IEnumerable<Recorder> recorders, Facility facility);

        Task RecorderUnassignFromTeamAndChannels(ILogger logger, IEnumerable<string> recorderEmails, Facility facility, string calendarId);
    }

    internal class RecorderService: IRecorderService
    {
        private readonly IBreezyCosmosService _breezyCosmosService;
        private readonly IGraphTeamsMembersService _graphTeamsMembersService;
        private readonly IGraphTeamsChannelsService _graphTeamsChannelsService;
        private readonly IGraphTeamsChannelsMembersService _graphTeamsChannelsMembersService;
        private readonly IGraphUsersService _graphUsersService;
        private readonly TenantSettingsService _tenantSettingsService; //TODO: This need refactoring

        public RecorderService(
            IBreezyCosmosService breezyCosmosService,
            IGraphTeamsMembersService graphTeamsMembersService,
            IGraphTeamsChannelsService graphTeamsChannelsService,
            IGraphTeamsChannelsMembersService graphTeamsChannelsMembersService,
            IGraphUsersService graphUsersService)
        {
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));
            _graphTeamsMembersService = Guard.Against.Null(graphTeamsMembersService, nameof(graphTeamsMembersService));
            _graphTeamsChannelsService = Guard.Against.Null(graphTeamsChannelsService, nameof(graphTeamsChannelsService));
            _graphTeamsChannelsMembersService = Guard.Against.Null(graphTeamsChannelsMembersService, nameof(graphTeamsChannelsMembersService));
            _graphUsersService = Guard.Against.Null(graphUsersService, nameof(graphUsersService));
            _tenantSettingsService = new TenantSettingsService(breezyCosmosService);
        }

        /// <summary>
        /// Adds a list of recorders to the facility team and any channels they have access to
        /// </summary>
        public async Task RecorderAssignToTeamAndChannels(ILogger logger, IEnumerable<Recorder> recorders, Facility facility)
        {
            // Check if recorders are already a member of the team associated with the facility
            var members = await _graphTeamsMembersService.TeamMembersList(AuthenticationType.GraphService, facility.Team.MsTeamId);
            var recordersToAddToTeam = new List<Recorder>();
            foreach (var recorder in recorders)
            {
                if (!members.Select(m => m.Id).Contains(recorder.MsAadId))
                {
                    recordersToAddToTeam.Add(recorder);
                }
            }

            // Add recorders to team if required
            if (recordersToAddToTeam.Any())
            {
                await _graphTeamsMembersService.TeamMemberCreateBulk(AuthenticationType.ScheduledEventService, facility.Team.MsTeamId, recordersToAddToTeam.Select(u => u.MsAadId));
            }

            // Add recorders to the channel(s) they have access to
            foreach (var settingsChannel in (await _tenantSettingsService.GetTenantSettings()).Channels.Where(c => c.MembershipType == "private"))
            {
                var recordersWithAccess = recorders.Where(r => settingsChannel.AccessLevels.Contains(r.AccessLevel));
                if (recordersWithAccess.Any())
                {
                    var facilityChannel = facility.Team.Channels.FirstOrDefault(c => c.Name == settingsChannel.Name);
                    if (facilityChannel != null)
                    {
                        var successCount = await _graphTeamsChannelsMembersService.TeamChannelMemberCreateBulk(AuthenticationType.GraphService, facility.Team.MsTeamId, facilityChannel.MsChannelId, recordersWithAccess.Select(u => u.MsAadId));
                        var failCount = recordersWithAccess.Count() - successCount;
                        if (failCount > 0) logger.LogError("Failed to add {failCount} recorder(s) to channel '{ChannelName}' in team '{TeamName}'",
                            failCount, facilityChannel.Name, facility.Team.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a list of recorders from the facility team and any channels associated channels
        /// </summary>
        public async Task RecorderUnassignFromTeamAndChannels(ILogger logger, IEnumerable<string> recorderEmails, Facility facility, string calendarId)
        {
            var recorders = await _breezyCosmosService.ListRecorders(recorderEmails);
            if (!recorders.Any()) return;

            // Get all calendars linked to this facility
            var relatedCalendars = await _breezyCosmosService.ListCalendars(facility.Id);

            // Determine which recorders can be removed
            var recordersToRemoveFromChannels = new List<Recorder>();
            foreach (var recorder in recorders)
            {
                // We don't want to remove a recorder from team channels if it's linked to another calendar
                var linkedCalendars = relatedCalendars.Where(c => c.Id != calendarId && c.Recorders.Contains(recorder.Email));
                if (!linkedCalendars.Any()) recordersToRemoveFromChannels.Add(recorder);
            }

            var msUserIdsToRemove = recordersToRemoveFromChannels.Select(r => r.MsAadId);
            if (msUserIdsToRemove.Any())
            {
                // Get all private channels for this team which match the facility channels
                var teamChannels = (await _graphTeamsChannelsService.TeamChannelsList(AuthenticationType.GraphService, facility.Team.MsTeamId))
                    .Where(msChannel => msChannel.MembershipType == Microsoft.Graph.ChannelMembershipType.Private && facility.Team.Channels.Select(facilityChannel => facilityChannel.Name).Contains(msChannel.DisplayName));

                // Remove recorder users from each channel
                foreach (var msChannel in teamChannels)
                {
                    var allMembers = await _graphTeamsChannelsMembersService.TeamChannelMemberList(AuthenticationType.ScheduledEventService, facility.Team.MsTeamId, msChannel.Id);
                    var membersToDelete = allMembers.Where(m => msUserIdsToRemove.Contains((m as Microsoft.Graph.AadUserConversationMember).UserId));
                    await _graphTeamsChannelsMembersService.TeamChannelMemberDeleteBulk(AuthenticationType.ScheduledEventService, facility.Team.MsTeamId, msChannel.Id, membersToDelete.Select(m => m.Id));
                }
            }

            // Get team members corresponding to the recorders
            var allTeamMembers = await _graphTeamsMembersService.TeamMembersList(AuthenticationType.GraphService, facility.Team.MsTeamId);
            var teamMembersToRemove = allTeamMembers.Where(m => msUserIdsToRemove.Contains((m as Microsoft.Graph.AadUserConversationMember).UserId));

            // Remove team members from facility team
            if (teamMembersToRemove.Any())
                await _graphTeamsMembersService.TeamMemberDeleteBulk(AuthenticationType.GraphService, facility.Team.MsTeamId, teamMembersToRemove.Select(m => m.Id));
        }
    }
}
