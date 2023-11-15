using Ardalis.GuardClauses;
using Breezy.Muticaster.TenantSettings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface IUserService
    {
        Task UserAssignToTeamAndChannels(ILogger logger, IEnumerable<string> userEmails, Facility facility);

        Task UserAssignToTeamAndChannels(ILogger logger, IEnumerable<User> users, Facility facility);

        Task UserUnassignFromTeamAndChannels(ILogger logger, IEnumerable<string> userEmails, string facilityId, string calendarId);

        Task UserUnassignFromTeamAndChannels(ILogger logger, IEnumerable<string> userEmails, Facility facility, string calendarId);
    }

    internal class UserService: IUserService
    {
        private readonly IBreezyCosmosService _breezyCosmosService;
        private readonly IGraphTeamsMembersService _graphTeamsMembersService;
        private readonly IGraphTeamsChannelsService _graphTeamsChannelsService;
        private readonly IGraphTeamsChannelsMembersService _graphTeamsChannelsMembersService;
          private readonly TenantSettingsService _tenantSettingsService; //TODO: This need refactoring  


        public UserService(
            IBreezyCosmosService breezyCosmosService,           
            IGraphTeamsMembersService graphTeamsMembersService,
            IGraphTeamsChannelsService graphTeamsChannelsService,
            IGraphTeamsChannelsMembersService graphTeamsChannelsMembersService)
        {
            _breezyCosmosService = Guard.Against.Null(breezyCosmosService, nameof(breezyCosmosService));           
            _graphTeamsMembersService = Guard.Against.Null(graphTeamsMembersService, nameof(graphTeamsMembersService));
            _graphTeamsChannelsService = Guard.Against.Null(graphTeamsChannelsService, nameof(graphTeamsChannelsService));
            _graphTeamsChannelsMembersService = Guard.Against.Null(graphTeamsChannelsMembersService, nameof(graphTeamsChannelsMembersService));            
            _tenantSettingsService = new TenantSettingsService(breezyCosmosService);
        }

        /// <summary>
        /// Adds a list of users to the facility team and any channels they have access to
        /// </summary>
        public async Task UserAssignToTeamAndChannels(ILogger logger, IEnumerable<string> userEmails, Facility facility)
        {
            await UserAssignToTeamAndChannels(logger, await _breezyCosmosService.ListUsers(userEmails), facility);
        }

        /// <summary>
        /// Adds a list of users to the facility team and any channels they have access to
        /// </summary>
        public async Task UserAssignToTeamAndChannels(ILogger logger, IEnumerable<User> users, Facility facility)
        {
            // Check if users are already a member of the team associated with the facility
            var members = await _graphTeamsMembersService.TeamMembersList(AuthenticationType.GraphService, facility.Team.MsTeamId);
            var usersToAddToTeam = new List<User>();
            foreach (var user in users)
            {
                if (!members.Select(m => m.Id).Contains(user.MsAadId))
                {
                    usersToAddToTeam.Add(user);
                }
            }

            // Add users to team if required
            if (usersToAddToTeam.Any())
            {
                await _graphTeamsMembersService.TeamMemberCreateBulk(AuthenticationType.ScheduledEventService, facility.Team.MsTeamId, usersToAddToTeam.Select(u => u.MsAadId));
            }

            // Add users to the channel(s) they have access to
            foreach (var settingsChannel in (await _tenantSettingsService.GetTenantSettings()).Channels.Where(c => c.MembershipType == "private"))
            {
                var usersWithAccess = users.Where(u => settingsChannel.AccessLevels.Contains(u.AccessLevel));
                if (usersWithAccess.Any())
                {
                    var facilityChannel = facility.Team.Channels.FirstOrDefault(c => c.Name == settingsChannel.Name);
                    if (facilityChannel != null)
                    {
                        var successCount = await _graphTeamsChannelsMembersService.TeamChannelMemberCreateBulk(AuthenticationType.GraphService, facility.Team.MsTeamId, facilityChannel.MsChannelId, usersWithAccess.Select(u => u.MsAadId));
                        var failCount = usersWithAccess.Count() - successCount;
                        if (failCount > 0) logger.LogError("Failed to add {failCount} user(s) to channel '{ChannelName}' in team '{TeamName}'",
                            failCount, facilityChannel.Name, facility.Team.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a list of users from the facility team and any channels associated channels
        /// </summary>
        public async Task UserUnassignFromTeamAndChannels(ILogger logger, IEnumerable<string> userEmails, string facilityId, string calendarId)
        {
            await UserUnassignFromTeamAndChannels(logger, userEmails, await _breezyCosmosService.GetFacility(facilityId), calendarId);
        }

        /// <summary>
        /// Removes a list of users from the facility team and any channels associated channels
        /// </summary>
        public async Task UserUnassignFromTeamAndChannels(ILogger logger, IEnumerable<string> userEmails, Facility facility, string calendarId)
        {
            var focusUsers = await _breezyCosmosService.ListUsers(userEmails);
            if (!focusUsers.Any()) return;

            // Get all calendars linked to this facility
            var relatedCalendars = await _breezyCosmosService.ListCalendars(facility.Id);

            // Determine which users can be removed
            var focusUsersToRemoveFromChannels = new List<User>();
            foreach (var focusUser in focusUsers)
            {
                // We don't want to remove a user from team channels if it's linked to another calendar
                var linkedCalendars = relatedCalendars.Where(c => c.Id != calendarId && c.focusUsers.Contains(focusUser.Email));
                if (!linkedCalendars.Any()) focusUsersToRemoveFromChannels.Add(focusUser);
            }

            var msUserIdsToRemove = focusUsersToRemoveFromChannels.Select(r => r.MsAadId);
            if (msUserIdsToRemove.Any())
            {
                // Get all private channels for this team which match the facility channels
                var teamChannels = (await _graphTeamsChannelsService.TeamChannelsList(AuthenticationType.GraphService, facility.Team.MsTeamId))
                    .Where(msChannel => msChannel.MembershipType == Microsoft.Graph.ChannelMembershipType.Private && facility.Team.Channels.Select(facilityChannel => facilityChannel.Name).Contains(msChannel.DisplayName));

                // Remove focus users from each channel
                foreach (var msChannel in teamChannels)
                {
                    var allMembers = await _graphTeamsChannelsMembersService.TeamChannelMemberList(AuthenticationType.ScheduledEventService, facility.Team.MsTeamId, msChannel.Id);
                    var membersToDelete = allMembers.Where(m => msUserIdsToRemove.Contains((m as Microsoft.Graph.AadUserConversationMember).UserId));
                    await _graphTeamsChannelsMembersService.TeamChannelMemberDeleteBulk(AuthenticationType.ScheduledEventService, facility.Team.MsTeamId, msChannel.Id, membersToDelete.Select(m => m.Id));
                }
            }

            // Get team members corresponding to the focus users
            var allTeamMembers = await _graphTeamsMembersService.TeamMembersList(AuthenticationType.GraphService, facility.Team.MsTeamId);
            var teamMembersToRemove = allTeamMembers.Where(m => msUserIdsToRemove.Contains((m as Microsoft.Graph.AadUserConversationMember).UserId));

            // Remove team members from facility team
            if (teamMembersToRemove.Any())
                await _graphTeamsMembersService.TeamMemberDeleteBulk(AuthenticationType.GraphService, facility.Team.MsTeamId, teamMembersToRemove.Select(m => m.Id));
        }

    }
}
