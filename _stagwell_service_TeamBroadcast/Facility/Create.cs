using Breezy.Muticaster.TenantSettings;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public partial class Facility
    {
        [OpenApiOperation(operationId: "facilities-post", Summary = "/facilities - POST", Description = "Add a facility")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(FacilityCreateUpdateParams), Required = true, Description = "The facility to add")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "application/json", bodyType: typeof(Facility), Summary = "Created OK", Description = "The facility was created successfully")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Summary = "Not Found", Description = "The provided building was not found")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Conflict, contentType: "text/plain", bodyType: typeof(string), Summary = "Conflict", Description = "The facility passed already exists")]
        [RequiredPermission("facilities.ReadWriteAll")]
        [Function("Facility_Create")]
        public async Task<HttpResponseData> FacilityCreate([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "facilities")] HttpRequestData req, FunctionContext executionContext)
        {
            var functionName = nameof(FacilityCreate);
            var logger = executionContext.GetLogger(functionName);
            logger.LogInformation("Start of {functionName} function", functionName);
            if (!await _authService.CheckAuthorisation(req, logger)) return await req.UnauthorisedResponse(logger);

            try
            {
                // Parse the input object in the HTTP request body, return status 400 if it isn't there
                var input = req.GetBodyObject<FacilityCreateUpdateParams>(out List<ValidationResult> validationResults);
                if (input == null)
                {
                    logger.LogWarning("Missing or invalid parameters.");
                    return await req.BadRequestResponse(logger, validationResults);
                }

                // Create facility id (or use existing)
                var facilityId = (string.IsNullOrWhiteSpace(input.Id) ? Guid.NewGuid().ToString() : input.Id).ToLower();

                // Check facility exists with above id, return status 409 if true
                var existingFacility = await _breezyCosmosService.GetFacility(facilityId);
                if (existingFacility != null)
                {
                    return await req.ConflictResponse(logger, $"A facility with id '{facilityId}' already exists.");
                }

                // Check building exists
                var building = await _breezyCosmosService.GetItem<LocationService.BuildingLocation>(breezyContainers.Locations, input.BuildingId, input.BuildingId);
                if (building == null)
                {
                    return await req.NotFoundResponse(logger, $"A building with id {input.BuildingId} was not found");
                }

                // Check displayName has been specified; create one if not
                var displayName = string.IsNullOrWhiteSpace(input.DisplayName) ? BuildFacilityName(building.RegionName, building.Name, input.Room) : input.DisplayName;

                // Create team
                var team = await MsTeamCreate(input.DisplayName, TeamType.Facility, logger);

                // Insert document in Cosmos container
                var facility = new Facility
                {
                    BuildingId = building.Id,
                    BuildingName = building.Name,
                    CountryId = building.CountryId,
                    CountryName = building.CountryName,
                    DisplayName = displayName,
                    FacilityType = input.FacilityType,
                    Floor = input.Floor,
                    Id = facilityId,
                    RegionId = building.RegionId,
                    RegionName = building.RegionName,
                    Room = input.Room,
                    StateId = building.StateId,
                    StateName = building.StateName,
                    SubRegionId = building.SubRegionId,
                    SubRegionName = building.SubRegionName,
                    Team = team
                };
                await _breezyCosmosService.CreateItem(breezyContainers.Facilities, facility, facilityId);

                logger.LogInformation("Function {functionName} succeeded!", functionName);
                return await req.CreatedResponse(facility);
            }
            catch (Exception ex)
            {
                return await req.ExceptionResponse(logger, ex);
            }
        }

        private async Task<FacilityTeamDetails> MsTeamCreate(string displayName, TeamType teamType, ILogger logger)
        {
            // Create team
            logger.LogInformation("Creating {teamType} team for {displayName}...", teamType, displayName);
            var teamId = await _graphTeamsService.Create(AuthenticationType.ScheduledEventService, displayName, $"{teamType} team for {displayName}", "private");
            if (teamId == null) throw new Exception("Timeout creating new Team");
            logger.LogInformation("Successfully created team with id {teamId}", teamId);

            // Create MS Channel for each channel in the tenant settings
            var settings = await _tenantSettingsService.GetTenantSettings();
            var channelsToCreate = settings.Channels.Select(c => new Microsoft.Graph.Channel()
            {
                DisplayName = c.Name,
                IsFavoriteByDefault = c.MembershipType == "standard",
                MembershipType = Enum.Parse<Microsoft.Graph.ChannelMembershipType>(c.MembershipType, true)
            });
            // Create MS Channel for each channel in the list
            logger.LogInformation("Creating {count} channels...", channelsToCreate.Count());
            
            var createdChannels = await _graphTeamsChannelsService.TeamChannelCreateBulk(AuthenticationType.ScheduledEventService, teamId, channelsToCreate);
            
            logger.LogInformation("Successfully created {count} channels: {channelIds}", 
                createdChannels.Count(), string.Join(", ", createdChannels.Select(c => c.Id)));

            if (channelsToCreate.Count() > createdChannels.Count())
            {
                var missingChannels = channelsToCreate.Where(c => !createdChannels.Select(created => created.DisplayName).Contains(c.DisplayName));
                logger.LogWarning("Failed to create {count} channels: {displayNames}", 
                    missingChannels.Count(), string.Join(", ", missingChannels.Select(c => c.DisplayName)));
            }

            // Install app and channel tabs for meetings channel
            var installedApps = new List<FacilityTeamAppDetails>();
            var meetingsChannel = createdChannels.FirstOrDefault(c => c.DisplayName == "Meetings");
            if (meetingsChannel == null)
            {
                logger.LogInformation("Meetings channel is not defined, skipping teams app and channel tab creation");
            }
            else
            {
                logger.LogInformation("Found {count} meetings channel apps, installing...", settings.MeetingsChannelApps.Length);
                int successCount = 0, failCount = 0;
                foreach (var meetingsChannelApp in settings.MeetingsChannelApps)
                {
                    // Install app
                    var appInstallation = await _graphTeamsService.InstallApp(AuthenticationType.ScheduledEventService, teamId, meetingsChannelApp.TeamsAppId);
                    if (appInstallation == null)
                    {
                        logger.LogError("There was a problem installing app with id '{TeamsAppId}' in team '{teamId}'", meetingsChannelApp.TeamsAppId, teamId);
                        continue;
                    }
                    installedApps.Add(new FacilityTeamAppDetails { MsChannelId = meetingsChannel.Id, MsTeamsAppId = meetingsChannelApp.TeamsAppId, MsTeamsAppInstallationId = appInstallation.Id });
                    logger.LogInformation("Successfully installed app with id '{TeamsAppId}' in team '{teamId}'", meetingsChannelApp.TeamsAppId, teamId);

                    // Create channel tab for app
                    var teamsTab = await _graphTeamsChannelsService.TeamChannelTabsCreate(
                        AuthenticationType.ScheduledEventService, 
                        teamId, 
                        meetingsChannel.Id,
                        meetingsChannelApp.TeamsAppId,
                        meetingsChannelApp.DisplayName, 
                        meetingsChannelApp.ContentUrl);

                    if (teamsTab != null)
                    {
                        logger.LogInformation("Successfully created tab '{DisplayName}' in channel '{Id}' in team '{teamId}'", meetingsChannel.DisplayName, meetingsChannel.Id, teamId);
                        successCount++;
                    }
                    else
                    {
                        logger.LogError("Unable to create tab '{DisplayName}' in channel '{Id}' in team '{teamId}': there was a timeout waiting for the channel to become active", meetingsChannel.DisplayName, meetingsChannel.Id, teamId);
                        failCount++;
                    }
                }
                if (successCount > 0) logger.LogInformation("Successfully created {successCount} channel tabs.", successCount);
                if (failCount > 0) logger.LogError("Failed to create {failCount} channel tabs.", failCount);
            }

            // Find a channel from the settings which matches the provided display name
            ChannelDescription getMatchingSettingsChannel(string displayName, IEnumerable<ChannelDescription> channels)
            {
                var retval = channels.FirstOrDefault(c => c.Name == displayName);
                if (retval == null) retval = new ChannelDescription()
                {
                    IsDefaultChannel = false,
                    AddRecorderUser = false
                };
                return retval;
            }

            return new FacilityTeamDetails()
            {
                MsTeamId = teamId,
                Name = displayName,
                Type = teamType,
                Apps = installedApps.ToArray(),
                Channels = createdChannels
                    .Select(c => new FacilityTeamChannelDetails()
                    {
                        AddRecorderUser = c.DisplayName != "General" && getMatchingSettingsChannel(c.DisplayName, settings.Channels).AddRecorderUser,
                        IsDefaultChannel = c.DisplayName != "General" && getMatchingSettingsChannel(c.DisplayName, settings.Channels).IsDefaultChannel,
                        MsChannelId = c.Id,
                        Name = c.DisplayName
                    })
                    .ToArray()
            };
        }
    }

    public class FacilityTeamDetails
    {
        [OpenApiProperty(Description = "The channels associated with this team")]
        [DataType(DataType.Custom)]
        [JsonProperty("channels")]
        [JsonRequired]
        public FacilityTeamChannelDetails[] Channels { get; set; }
        [OpenApiProperty(Description = "The installed apps associated with this team")]
        [DataType(DataType.Custom)]
        [JsonProperty("apps")]
        [JsonRequired]
        public FacilityTeamAppDetails[] Apps { get; set; }
        [OpenApiProperty(Description = "The Microsoft Graph team id")]
        [DataType(DataType.Text)]
        [JsonProperty("msTeamId")]
        [JsonRequired]
        public string MsTeamId { get; set; }
        [OpenApiProperty(Description = "The name of this team")]
        [DataType(DataType.Text)]
        [JsonProperty("name")]
        [JsonRequired]
        public string Name { get; set; }
        [OpenApiProperty(Description = "The team type")]
        [DataType(DataType.Text)]
        [JsonConverter(typeof(StringEnumConverter))]
        [EnumDataType(typeof(TeamType))]
        [JsonProperty("type")]
        [JsonRequired]
        public TeamType Type { get; set; }
    }

    public class FacilityTeamChannelDetails
    {
        [OpenApiProperty(Description = "Flag add recorder user", Nullable = true)]
        [DataType(DataType.Text)]
        [JsonProperty("addRecorderUser")]
        public bool? AddRecorderUser { get; set; }
        [OpenApiProperty(Description = "Flag whether this is the default channel", Nullable = true)]
        [DataType(DataType.Text)]
        [JsonProperty("isDefaultChannel")]
        public bool? IsDefaultChannel { get; set; }
        [OpenApiProperty(Description = "The Microsoft Graph channel id")]
        [DataType(DataType.Text)]
        [JsonProperty("msChannelId")]
        [JsonRequired]
        public string MsChannelId { get; set; }
        [OpenApiProperty(Description = "The name of the channel")]
        [DataType(DataType.Text)]
        [JsonProperty("name")]
        [JsonRequired]
        public string Name { get; set; }
    }

    public class FacilityTeamAppDetails
    {
        [OpenApiProperty(Description = "The Microsoft Graph channel id")]
        [DataType(DataType.Text)]
        [JsonProperty("msChannelId")]
        [JsonRequired]
        public string MsChannelId { get; set; }
        [OpenApiProperty(Description = "The Microsoft Graph teams app id")]
        [DataType(DataType.Text)]
        [JsonProperty("msTeamsAppId")]
        [JsonRequired]
        public string MsTeamsAppId { get; set; }
        [OpenApiProperty(Description = "The Microsoft Graph teams app installation id")]
        [DataType(DataType.Text)]
        [JsonProperty("msTeamsAppInstallationId")]
        [JsonRequired]
        public string MsTeamsAppInstallationId { get; set; }
    }
}