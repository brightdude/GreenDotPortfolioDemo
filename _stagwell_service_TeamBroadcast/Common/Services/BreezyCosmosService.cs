using Breezy.Muticaster.Schema;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Breezy.Muticaster
{
    public interface IBreezyCosmosService : ICosmosService
    {
        Task<Facility> GetFacility(string id);

        Task<Facility> GetFacilityByTeam(string msTeamId);

        Task<IEnumerable<Facility>> ListFacilities(IEnumerable<string> facilityIds = null);

        Task<Calendar> GetCalendar(string externalCalendarId);

        Task<IEnumerable<Calendar>> ListCalendarsWithfocusUser(string focusUserEmail);

        Task<IEnumerable<Calendar>> ListCalendars(string facilityId);

        Task<IEnumerable<Calendar>> ListCalendarsForTeam(string msTeamId);

        Task<User> GetUser(string email);

        Task<IEnumerable<User>> ListUsers(bool activeOnly = false);

        Task<IEnumerable<User>> ListUsers(IEnumerable<string> emailAddresses, bool activeOnly = false);

        Task<Event> GetEventByMeetingId(string msMeetingId, bool activeOnly = true);        

        Task<IEnumerable<Event>> ListEventsByExternalId(string externalId, bool activeOnly = false);

        Task<IEnumerable<Event>> ListEventsByCalendar(string calendarId);        

        //Task<OnDemandMeeting> GetOnDemandMeeting(string msTeamId, string msMeetingId);

        //Task<IEnumerable<OnDemandMeeting>> ListOnDemandMeetings(string msTeamId, bool activeOnly);

        Task<Recorder> GetRecorder(string email);

        Task<Recorder> GetRecorderById(string id);

        Task<Recorder> GetRecorderByLocation(string locationName);

        Task<IEnumerable<Recorder>> ListRecorders(IEnumerable<string> emailAddresses = null);

        Task<IdNameDto> GetDepartment(string departmentId);

        Task<IdNameDto> GetRecordingType(string recordingTypeId);

        Task<IdNameDto> GetStreamingType(string streamingTypeId);

        Task<IEnumerable<string>> ListRecorderProvisioningStatus();
    }

    internal static class breezyContainers
    {
        public const string Calendars = nameof(Calendars);
        public const string Departments = nameof(Departments);        
        public const string Events = nameof(Events);
        public const string Facilities = nameof(Facilities);
        public const string Locations = nameof(Locations); 
        public const string OnDemandMeetings = nameof(OnDemandMeetings);
        public const string PersonnelRoles = nameof(PersonnelRoles);       
        public const string Recorders = nameof(Recorders);
        public const string Settings = nameof(Settings);
        public const string Titles = nameof(Titles);
        public const string Users = nameof(Users);        
    }

    internal class breezyCosmosService : AbstractCosmosService, IBreezyCosmosService
    {
        public breezyCosmosService(
            IOptionsMonitor<CosmosOptions> options,
            ICosmosClientFactory cosmosClientFactory,
            ILogger<breezyCosmosService> logger)
            : base(options.CurrentValue.DatabaseName, cosmosClientFactory, logger)
        {
        }

        //TODO: review naming and to lower
        public Task<Facility> GetFacility(string id)
        {
            return GetItem<Facility>(breezyContainers.Facilities, id.ToLower(), id.ToLower());
        }

        public Task<Facility> GetFacilityByTeam(string msTeamId)
        {
            var query = new QueryDefinition("SELECT * FROM Facilities c where c.team.msTeamId = @msTeamId")
                .WithParameter("@msTeamId", msTeamId);

            return GetItem<Facility>(breezyContainers.Facilities, query);
        }

        public Task<IEnumerable<Facility>> ListFacilities(IEnumerable<string> facilityIds = null)
        {
            var queryBuilder = new StringBuilder("SELECT * FROM f");

            if (!facilityIds.IsEmpty())
            {
                var values = facilityIds.Select(id => $"'{id}'");
                queryBuilder.Append($" WHERE f.id IN ({string.Join(",", values)})");
            }

            return GetList<Facility>(breezyContainers.Facilities, new QueryDefinition(queryBuilder.ToString()));
        }

        public Task<Calendar> GetCalendar(string externalCalendarId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.externalCalendarId = @externalCalendarId")
                .WithParameter("@externalCalendarId", externalCalendarId.ToLower());

            return GetItem<Calendar>(breezyContainers.Calendars, query);
        }

        public Task<IEnumerable<Calendar>> ListCalendarsWithfocusUser(string focusUserEmail)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE ARRAY_CONTAINS(c.focusUsers, @email)")
                .WithParameter("@email", focusUserEmail);

            return GetList<Calendar>(breezyContainers.Calendars, query);
        }

        public Task<IEnumerable<Calendar>> ListCalendars(string facilityId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.facilityId = @facilityId")
                .WithParameter("@facilityId", facilityId.ToLower());

            return GetList<Calendar>(breezyContainers.Calendars, query);
        }

        public Task<IEnumerable<Calendar>> ListCalendarsForTeam(string msTeamId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.msTeamId = @msTeamId")
                .WithParameter("@msTeamId", msTeamId);

            return GetList<Calendar>(breezyContainers.Calendars, query);
        }

        public Task<User> GetUser(string email)
        {
            var query = new QueryDefinition("SELECT * FROM u WHERE u.email = @email")
                .WithParameter("@email", email.ToLower());

            return GetItem<User>(breezyContainers.Users, query, email.ToLower());
        }

        public Task<IEnumerable<User>> ListUsers(bool activeOnly = false)
        {
            return ListUsers(null, activeOnly);
        }

        //TODO: Review to lower
        public Task<IEnumerable<User>> ListUsers(IEnumerable<string> emailAddresses, bool activeOnly = false)
        {
            var queryBuilder = new StringBuilder("SELECT * FROM u");

            if (!emailAddresses.IsEmpty())
            {
                var values = emailAddresses.Select(e => $"'{e.ToLower()}'");
                queryBuilder.Append($" WHERE LOWER(u.email) IN ({string.Join(',', values)})");

                if (activeOnly)
                {
                    queryBuilder.Append(" AND u.activeFlag = true");
                }
            }
            else if (activeOnly)
            {
                queryBuilder.Append(" WHERE u.activeFlag = true");
            }

            return GetList<User>(breezyContainers.Users, new QueryDefinition(queryBuilder.ToString()));
        }

        public Task<Event> GetEventByMeetingId(string msMeetingId, bool activeOnly = true)
        {
            var queryBuilder = new StringBuilder("SELECT * FROM c WHERE c.msMeetingId = @msMeetingId");

            if (activeOnly)
            {
                queryBuilder.Append(" AND c.status = 'Active'");
            }

            var queryDef = new QueryDefinition(queryBuilder.ToString())
                .WithParameter("@msMeetingId", msMeetingId);

            return GetItem<Event>(breezyContainers.Events, queryDef);
        }

        public Task<IEnumerable<Event>> ListEventsByExternalId(string externalId, bool activeOnly = false)
        {
            var queryBuilder = new StringBuilder("SELECT * FROM c WHERE c.externalId = @externalId");

            if (activeOnly)
            {
                queryBuilder.Append(" AND c.status = 'Active'");
            }

            var query = new QueryDefinition(queryBuilder.ToString())
                .WithParameter("@externalId", externalId.ToLower());

            return GetList<Event>(breezyContainers.Events, query);
        }

        public Task<IEnumerable<Event>> ListEventsByCalendar(string calendarId)
        {
            var query = new QueryDefinition("SELECT * FROM e WHERE e.calendarId = @calendarId")
                .WithParameter("@calendarId", calendarId.ToLower());

            return GetList<Event>(breezyContainers.Events, query);
        }


        public Task<Recorder> GetRecorder(string email)
        {
            var query = new QueryDefinition("SELECT * FROM r WHERE r.email = @email")
                .WithParameter("@email", email);

            return GetItem<Recorder>(breezyContainers.Recorders, query);
        }

        public Task<Recorder> GetRecorderById(string id)
        {
            var query = new QueryDefinition("SELECT * FROM r WHERE r.id = @id")
                .WithParameter("@id", id);

            return GetItem<Recorder>(breezyContainers.Recorders, query, id);
        }

        public Task<Recorder> GetRecorderByLocation(string locationName)
        {
            var query = new QueryDefinition("SELECT * FROM r WHERE r.locationName = @locationName")
                .WithParameter("@locationName", locationName);

            return GetItem<Recorder>(breezyContainers.Recorders, query);
        }

        public Task<IEnumerable<Recorder>> ListRecorders(IEnumerable<string> emailAddresses = null)
        {
            if (emailAddresses.IsEmpty())
            {
                return GetList<Recorder>(breezyContainers.Recorders, new QueryDefinition("SELECT * FROM c"));
            }

            var values = emailAddresses.Select(e => $"'{e.ToLower()}'");
            var query = new QueryDefinition($"SELECT * FROM r WHERE LOWER(r.email) IN ({string.Join(',', values)})");

            return GetList<Recorder>(breezyContainers.Recorders, query);
        }

        public Task<IdNameDto> GetDepartment(string departmentId)
        {
            var query = new QueryDefinition("SELECT * FROM d WHERE d.id = @departmentId AND d.status = 'Active'")
                .WithParameter("@departmentId", departmentId);

            return GetItem<IdNameDto>(breezyContainers.Departments, query);
        }

        public Task<IdNameDto> GetRecordingType(string recordingTypeId)
        {
            var query = new QueryDefinition("SELECT VALUE r FROM c join r in c.recordingTypeValues WHERE r.id = @recordingTypeId AND r.status = 'Active'")
                .WithParameter("@recordingTypeId", recordingTypeId);

            return GetItem<IdNameDto>(breezyContainers.Settings, query);
        }

        public Task<IdNameDto> GetStreamingType(string streamingTypeId)
        {
            var query = new QueryDefinition("SELECT VALUE s FROM c join s in c.streamTypeValues WHERE s.id = @streamingTypeId AND s.status = 'Active'")
                .WithParameter("@streamingTypeId", streamingTypeId);

            return GetItem<IdNameDto>(breezyContainers.Settings, query);
        }

        public Task<IEnumerable<string>> ListRecorderProvisioningStatus()
        {
            var query = new QueryDefinition("SELECT VALUE v FROM v IN s.recorderProvisioningStatusValues");
            return GetList<string>(breezyContainers.Settings, query);
        }
    }
}
