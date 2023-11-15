import { expect } from "chai";
import { CosmosDbHelper, ICalendarCreateUpdateParams, IFacilityChannel, IRecorder, IUser } from "../../helpers/cosmos-helper";
import { GraphHelper } from "../../helpers/graph-helper";
import { TenantSettingsHelper } from "../../helpers/tenant-settings-helper";
import { FacilityHelper } from "../facility/facility-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { CalendarHelper } from "./calendar-helper";
import { RecorderHelper } from "../recorder/recorder-helper";
import { EventHelper } from "../event/event-helper";

const calendarHelper = new CalendarHelper();
const recorderHelper = new RecorderHelper();
const facilityHelper = new FacilityHelper();
const dbHelper = new CosmosDbHelper();

describe("Calendar", async function (): Promise<void> {

    before(async function () {
        console.log("Calendar Before...");
    });

    after(async function () {
        console.log("Calendar After...");
        //await dbHelper.deleteTestData(false, ["Departments", "PersonnelRoles", "Titles", "Locations"]);
    });

    this.timeout(360000);

    it("Calendar - Retrieve all", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility();

        // CREATE calendars
        const calendar1 = await dbHelper.createTestCalendar({ facility });
        const calendar2 = await dbHelper.createTestCalendar({ facility });

        // GET calendars
        const getResponse = await calendarHelper.getCalendars();
        expect(getResponse.data.length).is.greaterThanOrEqual(2);

        const retrievedCalendar1 = getResponse.data.filter(c => c.id === calendar1.id).pop()!;
        expect(retrievedCalendar1).is.not.undefined;
        expect(retrievedCalendar1.id).equals(calendar1.id);
        expect(retrievedCalendar1.externalCalendarId).equals(calendar1.externalCalendarId);
        expect(retrievedCalendar1.facilityId).equals(facility.id);
        expect(retrievedCalendar1.facilityName).equals(facility.displayName);
        expect(retrievedCalendar1.personnelCount).equals(1);
        expect(retrievedCalendar1.recorderCount).equals(1);

        const retrievedCalendar2 = getResponse.data.filter(c => c.id === calendar2.id).pop()!;
        expect(retrievedCalendar2).is.not.undefined;
        expect(retrievedCalendar2.id).equals(calendar2.id);
        expect(retrievedCalendar2.externalCalendarId).equals(calendar2.externalCalendarId);
        expect(retrievedCalendar2.facilityId).equals(facility.id);
        expect(retrievedCalendar2.facilityName).equals(facility.displayName);
        expect(retrievedCalendar2.personnelCount).equals(1);
        expect(retrievedCalendar2.recorderCount).equals(1);
    });

    it("Calendar - Retrieve", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility();

        // CREATE user
        const user = await dbHelper.createTestUser();

        // CREATE recorder
        const recorder = await dbHelper.createTestRecorder();

        // CREATE calendar
        const calendar = await dbHelper.createTestCalendar({ facility, focusUsers: [user], recorders: [recorder] });

        // RETRIEVE calendar
        const getResponse = await calendarHelper.getCalendar(calendar.externalCalendarId);
        expect(getResponse.data).is.not.undefined;
        const retrievedCalendar = getResponse.data!;

        expect(retrievedCalendar.id).equals(calendar.id);
        expect(retrievedCalendar.externalCalendarId).equals(calendar.externalCalendarId);
        expect(retrievedCalendar.facilityId).equals(facility.id);
        expect(retrievedCalendar.msTeamId).equals(facility.team.msTeamId);
        expect(retrievedCalendar.focusUsers).is.not.undefined;
        expect(retrievedCalendar.focusUsers).to.have.lengthOf(1);
        expect(retrievedCalendar.focusUsers![0]).equals(user.email);
        expect(retrievedCalendar.recorders).is.not.undefined;
        expect(retrievedCalendar.recorders).to.have.lengthOf(1);
        expect(retrievedCalendar.recorders![0]).equals(recorder.email);
    });

    it("Calendar - Create", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE department
        const department = await dbHelper.createTestLookup("Departments", "department");

        // CREATE focus user
        const user = await dbHelper.createTestUser({ department, createMsUser: true });

        // CREATE recorder
        const recorder = await recorderHelper.createRecorder(department);

        // CREATE calendar
        const calendarRequest = await calendarHelper.createCalendarRequestBody({ facility, department, focusUsers: [user], recorders: [recorder] });
        const createResponse = await calendarHelper.createCalendar(calendarRequest);
        expect(createResponse.data).is.not.undefined;
        const createdCalendar = createResponse.data;
        expect(createdCalendar.calendarName).equals(calendarRequest.calendarName);
        expect(createdCalendar.focusUsers).includes(user.email);
        expect(createdCalendar.departmentId).equals(department.id);
        expect(createdCalendar.departmentName).equals(department.name);
        expect(createdCalendar.externalCalendarId).equals(calendarRequest.externalCalendarId);
        expect(createdCalendar.facilityId).equals(facility.id);
        expect(createdCalendar.holdingCalls).is.not.undefined;
        expect(createdCalendar.recorders).includes(recorder.email);

        // GET calendar
        const retrievedCalendar = (await dbHelper.getCalendar(createdCalendar.externalCalendarId))!;
        expect(retrievedCalendar).is.not.undefined;
        expect(retrievedCalendar.calendarName).equals(calendarRequest.calendarName);
        expect(retrievedCalendar.focusUsers).includes(user.email);
        expect(retrievedCalendar.departmentId).equals(department.id);
        expect(retrievedCalendar.departmentName).equals(department.name);
        expect(retrievedCalendar.externalCalendarId).equals(calendarRequest.externalCalendarId);
        expect(retrievedCalendar.facilityId).equals(facility.id);
        expect(retrievedCalendar.holdingCalls).is.not.undefined;
        expect(retrievedCalendar.recorders).includes(recorder.email);

        // Pause for 20 seconds before making graph API requests
        console.log("Pausing for 20 seconds...");
        await new Promise(resolve => setTimeout(resolve, 20000));

        // GET team members
        const graphHelper = new GraphHelper();
        const members = await graphHelper.TeamMembersList(facility.team.msTeamId);
        expect(members).is.not.undefined;
        expect(members.length).equals(3); // user + recorder + Automation service account
        expect(members.map(m => m.email)).includes(user.email);
        expect(members.map(m => m.email)).includes(recorder.email);

        // GET private channels with tier2 access
        const settingsChannels = (await TenantSettingsHelper.getChannels()).filter(c => c.membershipType === "private" && c.accessLevels.includes("tier2"));
        const settingsChannelsNames = settingsChannels.map(c => c.name);
        const teamChannels = facility.team.channels.filter(c => settingsChannelsNames.includes(c.name));
        expect(teamChannels.length).is.greaterThan(0);

        // GET members for each channel
        teamChannels.forEach(async channel => {
            const channelMembers = await graphHelper.TeamChannelMembersList(facility.team.msTeamId, channel.msChannelId);
            expect(channelMembers).is.not.undefined;
            expect(channelMembers.map(m => m.email)).includes(user.email);
            expect(channelMembers.map(m => m.email)).includes(recorder.email);
        });
    });

    it("Calendar - Create with empty users/recorders", async function (): Promise<void> {
        const calendarRequest = await calendarHelper.createCalendarRequestBody({ focusUsers: [], recorders: [] });
        await calendarHelper.createCalendar(calendarRequest);
    });

    it("Calendar - Update", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE department
        const department1 = await dbHelper.createTestLookup("Departments", "department");

        // CREATE focus user
        const user1 = await dbHelper.createTestUser({ department: department1, createMsUser: true });

        // CREATE recorder
        let recorder1 = await recorderHelper.createRecorder(department1);

        // CREATE calendar
        const calendar = await dbHelper.createTestCalendar({ facility, focusUsers: [user1], recorders: [recorder1], addUsersToTeam: true, department: department1 });

        // CREATE new department
        const department2 = await dbHelper.createTestLookup("Departments", "department");

        // CREATE new focus user
        const user2 = await dbHelper.createTestUser({ department: department2, createMsUser: true });

        // CREATE new recorder
        const recorder2 = await recorderHelper.createRecorder(department2);

        // UPDATE calendar
        const updatedName = calendar.calendarName + " UPDATED";
        const updateRequest: ICalendarCreateUpdateParams = {
            calendarName: updatedName,
            focusUsers: [user2.email],
            departmentId: department2.id,
            externalCalendarId: calendar.externalCalendarId,
            facilityId: facility.id,
            recorders: [recorder2.email]
        };
        const updateResponse = await calendarHelper.updateCalendar(calendar.externalCalendarId, updateRequest);
        expect(updateResponse.data).is.not.undefined;
        const updatedCalendar = updateResponse.data;

        expect(updatedCalendar.calendarName).equals(updatedName);
        expect(updatedCalendar.focusUsers!.length).equal(1);
        expect(updatedCalendar.focusUsers![0]).equals(user2.email);
        expect(updatedCalendar.departmentId).equals(department2.id);
        expect(updatedCalendar.departmentName).equals(department2.name);
        expect(updatedCalendar.recorders!.length).equals(1);
        expect(updatedCalendar.recorders![0]).equals(recorder2.email);

        // Pause for 30 seconds before fetching team members to avoid 404 from Graph API
        await new Promise(resolve => setTimeout(resolve, 30000));

        // GET team members
        const graphHelper = new GraphHelper();
        const members = await graphHelper.TeamMembersList(facility.team.msTeamId);
        expect(members).is.not.undefined;
        expect(members.length).equals(3); // user + recorder + Automation service account
        expect(members.map(m => m.email)).includes(user2.email);
        expect(members.map(m => m.email)).includes(recorder2.email);

        // GET private channels with tier2 access
        const settingsChannels = (await TenantSettingsHelper.getChannels()).filter(c => c.membershipType === "private" && c.accessLevels.includes("tier2"));
        const settingsChannelsNames = settingsChannels.map(c => c.name);
        const teamChannels = facility.team.channels.filter(c => settingsChannelsNames.includes(c.name));
        expect(teamChannels.length).is.greaterThan(0);

        // GET members for each channel
        teamChannels.forEach(async channel => {
            const channelMembers = await graphHelper.TeamChannelMembersList(facility.team.msTeamId, channel.msChannelId);
            expect(channelMembers).is.not.undefined;
            expect(channelMembers.map(m => m.email)).includes(user2.email);
            expect(channelMembers.map(m => m.email)).includes(recorder2.email);
        });
    });

    it("Calendar - Update - change facility", async function (): Promise<void> {
        // CREATE facility
        const facility1 = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE focus user
        const user1 = await dbHelper.createTestUser({ createMsUser: true });

        // CREATE calendar
        const calendar = await dbHelper.createTestCalendar({ facility: facility1, focusUsers: [user1], addUsersToTeam: true });

        // CREATE new facility
        const facility2 = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE new focus user
        const user2 = await dbHelper.createTestUser({ createMsUser: true });

        // UPDATE calendar
        const updateResponse = await calendarHelper.updateCalendar(calendar.externalCalendarId, { externalCalendarId: calendar.externalCalendarId, facilityId: facility2.id, focusUsers: [user1.email, user2.email] });
        expect(updateResponse.data).is.not.undefined;
        const updatedCalendar = updateResponse.data!;

        expect(updatedCalendar.externalCalendarId).equals(calendar.externalCalendarId);
        expect(updatedCalendar.facilityId).equals(facility2.id);
        expect(updatedCalendar.focusUsers).is.not.undefined;
        expect(updatedCalendar.focusUsers!).includes(user1.email);
        expect(updatedCalendar.focusUsers!).includes(user2.email);

        // Pause for 30 seconds before fetching team members to avoid 404 from Graph API
        await new Promise(resolve => setTimeout(resolve, 30000));

        // GET facility1 team members
        const graphHelper = new GraphHelper();
        const facility1Members = await graphHelper.TeamMembersList(facility1.team.msTeamId);
        expect(facility1Members.map(m => m.email)).does.not.include(user1.email);
        expect(facility1Members.map(m => m.email)).does.not.include(user2.email);

        // GET facility1 private channels with tier2 access
        const settingsChannels = (await TenantSettingsHelper.getChannels()).filter(c => c.membershipType === "private" && c.accessLevels.includes("tier2"));
        const settingsChannelsNames = settingsChannels.map(c => c.name);
        const team1Channels = facility1.team.channels.filter(c => settingsChannelsNames.includes(c.name));
        expect(team1Channels.length).is.greaterThan(0);

        // GET facility1 members for each channel
        team1Channels.forEach(async channel => {
            const channelMembers = await graphHelper.TeamChannelMembersList(facility1.team.msTeamId, channel.msChannelId);
            expect(channelMembers).is.not.undefined;
            expect(channelMembers.map(m => m.email)).does.not.include(user1.email);
            expect(channelMembers.map(m => m.email)).does.not.include(user2.email);
        });

        // GET facility2 team members
        const facility2Members = await graphHelper.TeamMembersList(facility2.team.msTeamId);
        expect(facility2Members.map(m => m.email)).includes(user1.email);
        expect(facility2Members.map(m => m.email)).includes(user2.email);

        // GET facility2 private channels with tier2 access
        const team2Channels = facility2.team.channels.filter(c => settingsChannelsNames.includes(c.name));
        expect(team2Channels.length).is.greaterThan(0);

        // GET facility2 members for each channel
        team2Channels.forEach(async channel => {
            const channelMembers = await graphHelper.TeamChannelMembersList(facility2.team.msTeamId, channel.msChannelId);
            expect(channelMembers).is.not.undefined;
            expect(channelMembers.map(m => m.email)).includes(user1.email);
            expect(channelMembers.map(m => m.email)).includes(user2.email);
        });
    });

    it("Calendar - Update - change facility with Event", async function (): Promise<void> {
                        
        const newFacility = await dbHelper.createTestFacility();
        const calendar = await dbHelper.createTestCalendar({focusUsers: [], recorders: []});
        
        const calEvent = await dbHelper.createTestEvent({calendar:calendar});          
        const updateCalendar = (await calendarHelper.updateCalendar(calendar.externalCalendarId, { externalCalendarId: calendar.externalCalendarId, facilityId: newFacility.id})).data;
        const updateEvent = (await new EventHelper().getEvent(calEvent.externalId)).data!;

        expect(updateCalendar.facilityId).equal(newFacility.id);
        expect(updateCalendar.facilityId).equal(updateEvent.facilityId);                
        expect(updateEvent.facilityId).equal(newFacility.id);
    });
    

    it("Calendar - Delete", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE focus users
        const user1 = await dbHelper.createTestUser({ createMsUser: true });
        const user2 = await dbHelper.createTestUser({ createMsUser: true });

        // CREATE calendar
        const calendar = await dbHelper.createTestCalendar({
            facility,
            focusUsers: [user1, user2],
            addUsersToTeam: true
        });

        // Pause for 30 seconds before fetching team members to avoid 404 from Graph API
        await new Promise(resolve => setTimeout(resolve, 30000));

        // GET team members
        const graphHelper = new GraphHelper();
        let members = await graphHelper.TeamMembersList(facility.team.msTeamId);
        expect(members.map(m => m.userId)).includes(user1.msAadId);
        expect(members.map(m => m.userId)).includes(user2.msAadId);

        // DELETE calendar
        await calendarHelper.deleteCalendar(calendar.externalCalendarId);

        // GET calendar
        const deletedCalendar = await dbHelper.getCalendar(calendar.externalCalendarId);
        expect(deletedCalendar).is.undefined;

        // GET team members
        members = await graphHelper.TeamMembersList(facility.team.msTeamId);
        expect(members.map(m => m.userId)).does.not.include(user1.msAadId);
        expect(members.map(m => m.userId)).does.not.include(user2.msAadId);
    });

    it("Calendar - Create, update, retrieve and delete", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE department
        const department = await dbHelper.createTestLookup("Departments", "department");

        // CREATE user
        const user = await dbHelper.createTestUser({ createMsUser: true });

        // CREATE recorder
        const recorder1 = await recorderHelper.createRecorder(department);

        // CREATE calendar
        const calendarRequest = await calendarHelper.createCalendarRequestBody({ facility, focusUsers: [user], recorders: [recorder1] })
        const createCalendarResponse = await calendarHelper.createCalendar(calendarRequest);
        const createdCalendar = createCalendarResponse.data;

        // Fetch list of private channels a tier2 user should have access to
        const channelsWithAccess: IFacilityChannel[] = [];
        (await TenantSettingsHelper.getChannels()).filter(c => c.membershipType === "private" && c.accessLevels.indexOf("tier2") > -1).forEach(settingsChannel => {
            const facilityChannel = facility.team.channels.filter(fc => fc.name === settingsChannel.name).pop();
            if (facilityChannel !== undefined) channelsWithAccess.push(facilityChannel);
        });

        // Pause for 30 seconds before fetching channel members to avoid 404 from Graph API
        await new Promise(resolve => setTimeout(resolve, 30000));

        // Check recorder is a member of all the expected channels
        const graphHelper = new GraphHelper();
        for (const channel of channelsWithAccess) {
            const channelMembers = await graphHelper.TeamChannelMembersList(facility.team.msTeamId, channel.msChannelId);
            expect(channelMembers.map(m => m.userId)).includes(recorder1.msAadId);
        }

        // GET facility and check calendar is returned
        const retrieveFacilityResponse = await facilityHelper.getFacility(facility.id);
        expect(retrieveFacilityResponse.data).is.not.undefined;
        const retrievedFacility = retrieveFacilityResponse.data!;
        expect(retrievedFacility.calendars).is.not.undefined;
        expect(retrievedFacility.calendars!.length).equals(1);
        expect(retrievedFacility.calendars![0].externalCalendarId).equals(createdCalendar.externalCalendarId);

        // UPDATE calendar with inactive department
        const inactiveDepartment = await dbHelper.createTestLookup("Departments", "department", true);
        await calendarHelper.updateCalendar(createdCalendar.externalCalendarId, { externalCalendarId: createdCalendar.externalCalendarId, departmentId: inactiveDepartment.id }, 404); // Inactive department should return 404

        // UPDATE calendar with active department
        const activeDepartment = await dbHelper.createTestLookup("Departments", "department");
        const updateCalendarResponse1 = await calendarHelper.updateCalendar(createdCalendar.externalCalendarId, { externalCalendarId: createdCalendar.externalCalendarId, departmentId: activeDepartment.id });
        expect(updateCalendarResponse1.data).is.not.undefined;
        const updatedCalendar1 = updateCalendarResponse1.data;
        expect(updatedCalendar1.externalCalendarId).equals(createdCalendar.externalCalendarId);
        expect(updatedCalendar1.departmentName).equals(activeDepartment.name);
        expect(updatedCalendar1.holdingCalls).is.not.undefined;

        // UPDATE calendar with new recorder
        const recorder2 = await recorderHelper.createRecorder(department);
        const updateCalendarResponse2 = await calendarHelper.updateCalendar(createdCalendar.externalCalendarId, { externalCalendarId: createdCalendar.externalCalendarId, recorders: [recorder2.email] });
        const updatedCalendar2 = updateCalendarResponse2.data;
        expect(updatedCalendar2.recorders).is.not.undefined;
        expect(updatedCalendar2.recorders!.length).equals(1);
        expect(updatedCalendar2.recorders![0]).equals(recorder2.email);

        // UPDATE calendar with different new recorder
        const recorder3 = await recorderHelper.createRecorder(department);
        const updateCalendarResponse3 = await calendarHelper.updateCalendar(createdCalendar.externalCalendarId, { externalCalendarId: createdCalendar.externalCalendarId, recorders: [recorder3.email] });
        const updatedCalendar3 = updateCalendarResponse3.data;
        expect(updatedCalendar3.recorders).is.not.undefined;
        expect(updatedCalendar3.recorders!.length).equals(1);
        expect(updatedCalendar3.recorders![0]).equals(recorder3.email);

        // Pause for 30 seconds before fetching channel members to avoid 404 from Graph API
        await new Promise(resolve => setTimeout(resolve, 30000));

        // Check recorder is a member of all the expected channels
        for (const channel of channelsWithAccess) {
            const channelMembers = await graphHelper.TeamChannelMembersList(facility.team.msTeamId, channel.msChannelId);
            expect(channelMembers.map(m => m.userId)).includes(recorder3.msAadId);
        }

        // GET calendar
        const getResponse = await calendarHelper.getCalendar(createdCalendar.externalCalendarId);
        expect(getResponse.data).is.not.undefined;
        const retrievedCalendar = getResponse.data!;
        expect(retrievedCalendar.recorders).is.not.undefined;
        expect(retrievedCalendar.recorders!.length).equals(1);
        expect(retrievedCalendar.recorders![0]).equals(recorder3.email);

        // GET calendar with case change
        await calendarHelper.getCalendar(createdCalendar.externalCalendarId.toUpperCase());
    });

    describe("Calendar Exceptions", async function (): Promise<void> {
        it("Calendar - Get - 404 unknown external ID", async function (): Promise<void> {
            await calendarHelper.getCalendar("UNKNOWN_EXTERNAL_ID", 404);
        });

        it("Calendar - Create - 404 unknown facility", async function (): Promise<void> {
            const request = await calendarHelper.createCalendarRequestBody();
            request.facilityId = "UNKNOWN_FACILITY_ID";
            await calendarHelper.createCalendar(request, 404);
        });

        it("Calendar - Create - 400 unknown user", async function (): Promise<void> {
            const unknownUser: IUser = {
                accessLevel: "tier3",
                id: LookupHelper.createTestGuid(),
                firstName: "Unknown",
                lastName: "User",
                displayName: "Unknown User",
                email: "UnknownUser@contoso.com",
                titleId: LookupHelper.createTestGuid(),
                titleName: "Mr",
                roleId: LookupHelper.createTestGuid(),
                roleName: "Unknown",
                departmentId: LookupHelper.createTestGuid(),
                departmentName: "Unknown",
                usageLocation: "US",
                activeFlag: true
            };
            const request = await calendarHelper.createCalendarRequestBody({ focusUsers: [unknownUser] });
            await calendarHelper.createCalendar(request, 400);
        });

        it("Calendar - Create - 404 unknown recorder", async function (): Promise<void> {
            const unknownRecorder: IRecorder = {
                id: LookupHelper.createTestGuid(),
                displayName: "Unknown Recorder",
                email: "UnknownRecorder@contoso.com",
                departmentId: LookupHelper.createTestGuid(),
                departmentName: "Unknown",
                locationName: "Unknown",
                provisioningStatus: "Unknown",
                recordingTypeId: LookupHelper.createTestGuid(),
                recordingTypeName: "Unknown",
                streamTypeId: LookupHelper.createTestGuid(),
                streamTypeName: "Unknown",
                status: "Unknown",
                accessLevel: "tier2",
                activeFlag: true
            };
            const request = await calendarHelper.createCalendarRequestBody({ recorders: [unknownRecorder] });
            await calendarHelper.createCalendar(request, 404);
        });

        it("Calendar - Create - 404 inactive recorder", async function (): Promise<void> {
            const inactiveRecorder = await dbHelper.createTestRecorder({ isInactive: true });
            const request = await calendarHelper.createCalendarRequestBody({ recorders: [inactiveRecorder] });
            await calendarHelper.createCalendar(request, 404);
        });

        it("Calendar - Create - 404 recorder exists in DB but not Graph", async function (): Promise<void> {
            const recorder = await dbHelper.createTestRecorder();
            const request = await calendarHelper.createCalendarRequestBody({ recorders: [recorder] });
            await calendarHelper.createCalendar(request, 404);
        });

        it("Calendar - Create - 409 duplicate", async function (): Promise<void> {
            const calendar = await dbHelper.createTestCalendar();
            await calendarHelper.createCalendar(calendar, 409);
        });

        it("Calendar - Update - 404 unknown calendar", async function (): Promise<void> {
            const calendar: ICalendarCreateUpdateParams = {
                externalCalendarId: LookupHelper.createTestGuid()
            };
            await calendarHelper.updateCalendar(calendar.externalCalendarId, calendar, 404);
        });

        it("Calendar - Update - 404 unknown facility", async function (): Promise<void> {
            const calendar = await dbHelper.createTestCalendar();
            calendar.facilityId = "UNKNOWN_FACILITY_ID";
            await calendarHelper.updateCalendar(calendar.externalCalendarId, calendar, 404);
        });

        it("Calendar - Update - 404 unknown recorder", async function (): Promise<void> {
            const calendar = await dbHelper.createTestCalendar();
            calendar.recorders = ["UnknownRecorder@contoso.com"];
            await calendarHelper.updateCalendar(calendar.externalCalendarId, calendar, 404);
        });

        it("Calendar - Update - 404 inactive recorder", async function (): Promise<void> {
            const calendar = await dbHelper.createTestCalendar();
            const inactiveRecorder = await dbHelper.createTestRecorder({ isInactive: true });
            const request: ICalendarCreateUpdateParams = {
                externalCalendarId: calendar.externalCalendarId,
                recorders: [inactiveRecorder.email]
            }
            await calendarHelper.updateCalendar(request.externalCalendarId, request, 404);
        });

        it("Calendar - Update - 400 unknown user", async function (): Promise<void> {
            const calendar = await dbHelper.createTestCalendar();
            calendar.focusUsers = ["UnknownUser@contoso.com"];
            await calendarHelper.updateCalendar(calendar.externalCalendarId, calendar, 400);
        });

        it("Calendar - Delete - 404 unknown external ID", async function (): Promise<void> {
            await calendarHelper.deleteCalendar("UNKNOWN_EXTERNAL_ID", 404);
        });
    });    
});