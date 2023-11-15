import { expect } from "chai";
import { CosmosDbHelper, IFacility, IFacilityCreateUpdateParams, ISettingChannel } from "../../helpers/cosmos-helper";
import { GraphAccessUserType, GraphHelper } from "../../helpers/graph-helper";
import { TenantSettingsHelper } from "../../helpers/tenant-settings-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { FacilityHelper } from "./facility-helper";

const facilityHelper = new FacilityHelper();
const dbHelper = new CosmosDbHelper();

describe("Facility", async function (): Promise<void> {

    before(async function () {
        console.log("Facility Before...");
    });

    after(async function () {
        console.log("Facility After...");
        //await dbHelper.deleteTestData(false, ["Departments", "PersonnelRoles", "Titles", "Locations"]);
    });

    this.timeout(360000);

    it("Facility - Retrieve all facilities", async function (): Promise<void> {
        // CREATE facility
        const newFacility = await dbHelper.createTestFacility();

        // GET facilities
        const retrieveResponse = await facilityHelper.getFacilities();
        const filteredFacilities = retrieveResponse.data.filter(f => f.id === newFacility.id);
        expect(filteredFacilities.length).equals(1);
        expect(filteredFacilities[0].id).equals(newFacility.id);
        expect(filteredFacilities[0].displayName).equals(newFacility.displayName);
    });

    it("Facility - Retrieve", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility();

        // CREATE calendar
        const calendar = await dbHelper.createTestCalendar({ facility });

        // CREATE on-demand meeting
        await dbHelper.createTestOnDemandMeeting(facility);

        // GET facility
        const retrieveResponse = await facilityHelper.getFacility(facility.id);
        expect(retrieveResponse.data).is.not.undefined;
        const retrievedFacility = retrieveResponse.data!;
        expect(retrievedFacility.id).equals(facility.id);
        expect(retrievedFacility.displayName).equals(facility.displayName);
        expect(retrievedFacility.calendars).is.not.undefined;
        expect(retrievedFacility.calendars!.length).equals(1);
        expect(retrievedFacility.calendars![0].externalCalendarId).equals(calendar.externalCalendarId);
        expect(retrievedFacility.activeOnDemandMeetingCount).equals(1);

        // GET with case change
        await facilityHelper.getFacility(facility.id.toUpperCase());
    });

    it("Facility - Create", async function (): Promise<void> {
        // CREATE building
        const building = await dbHelper.createTestBuilding();

        // CREATE facility
        const createRequest = await facilityHelper.createFacilityRequestBody({ building });
        const createResponse = await facilityHelper.createFacility(createRequest);

        // GET facility
        const retrievedFacility = (await dbHelper.getFacility(createResponse.data.id))!;

        expect(retrievedFacility.id).equals(createRequest.id);
        expect(retrievedFacility.floor).equals(createRequest.floor);
        expect(retrievedFacility.room).equals(createRequest.room);
        expect(retrievedFacility.displayName).equals(createRequest.displayName);
        expect(retrievedFacility.countryName).equals(building.countryName);
        expect(retrievedFacility.stateName).equals(building.stateName);
        expect(retrievedFacility.regionName).equals(building.regionName);
        expect(retrievedFacility.subRegionName).equals(building.subRegionName);
        expect(retrievedFacility.buildingName).equals(building.name);

        // GET team
        const graphHelper = new GraphHelper();
        const graphTeam = await graphHelper.TeamsGet(retrievedFacility.team.msTeamId);
        expect(graphTeam.displayName).equals(createRequest.displayName);

        // Pause for 30 seconds to let Graph get its act together
        await new Promise(resolve => setTimeout(resolve, 30000));

        const channelApps = await TenantSettingsHelper.getChannelApps();

        // GET apps in team
        const installedAppIds = await graphHelper.TeamsInstalledAppsList(retrievedFacility.team.msTeamId);
        retrievedFacility.team.apps.forEach(async app => {
            expect(installedAppIds).includes(app.msTeamsAppInstallationId);
        });

        // GET tabs in channel
        const meetingsChannel = retrievedFacility.team.channels.filter(c => c.name === "Meetings").pop();
        expect(meetingsChannel).is.not.undefined;
        const tabs = await graphHelper.TeamChannelTabsList(retrievedFacility.team.msTeamId, meetingsChannel!.msChannelId);
        channelApps.forEach(app => {
            expect(tabs.map(t => t.displayName)).includes(app.displayName);
        });
    });

    it("Facility - Update", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE building
        const building = await dbHelper.createTestBuilding();

        // UPDATE facility
        const updateRequest: IFacilityCreateUpdateParams = {
            id: facility.id,
            facilityType: facility.facilityType,
            buildingId: building.id,
            room: facility.room + " UPDATED",
            floor: facility.floor + " UPDATED",
            displayName: facility.displayName + " UPDATED"
        };
        await facilityHelper.updateFacility(facility.id, updateRequest);

        // GET facility
        const updatedFacility = (await dbHelper.getFacility(facility.id))!;

        expect(updatedFacility).is.not.undefined;
        expect(updatedFacility.id).equals(facility.id);
        expect(updatedFacility.floor).equals(updateRequest.floor);
        expect(updatedFacility.room).equals(updateRequest.room);
        expect(updatedFacility.displayName).equals(updateRequest.displayName);
        expect(updatedFacility.countryName).equals(building.countryName);
        expect(updatedFacility.stateName).equals(building.stateName);
        expect(updatedFacility.regionName).equals(building.regionName);
        expect(updatedFacility.subRegionName).equals(building.subRegionName);
        expect(updatedFacility.buildingName).equals(building.name);

        // GET team
        const graphHelper = new GraphHelper();
        const graphTeam = await graphHelper.TeamsGet(facility.team.msTeamId);
        expect(graphTeam.displayName).equals(updateRequest.displayName);
    });

    it("Facility - Delete", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE on-demand meeting
        const ondemandMeeting = await dbHelper.createTestOnDemandMeeting(facility);

        // GET online meeting
        const graphHelper = new GraphHelper();
        await graphHelper.OnlineMeetingGet(GraphAccessUserType.OnDemandService, ondemandMeeting.msMeetingId);

        // Pause for 10 seconds before deleting team to avoid 404 from Graph API
        await new Promise(resolve => setTimeout(resolve, 10000));

        // DELETE facility
        await facilityHelper.deleteFacility(facility.id);

        // GET facility
        const deletedFacility = await dbHelper.getFacility(facility.id);
        expect(deletedFacility).is.undefined;

        // GET on-demand meeting
        const deletedOndemandMeeting = await dbHelper.getOnDemandMeeting(facility.team.msTeamId);
        expect(deletedOndemandMeeting.activeFlag).is.false;

        // GET online meeting again
        await graphHelper.OnlineMeetingGet(GraphAccessUserType.ScheduledEventService, ondemandMeeting.msMeetingId, 404);
    });

    it("Facility - Create, update, retrieve and delete", async function (): Promise<void> {
        // CREATE building
        const building = await dbHelper.createTestBuilding();

        // CREATE facility
        const facilityId = LookupHelper.createTestGuid();
        const createRequestBody = await facilityHelper.createFacilityRequestBody({ facilityId, building });
        const createResponse = await facilityHelper.createFacility(createRequestBody);
        expect(createResponse.data).is.not.undefined;
        const createdFacility = createResponse.data;

        expect(createdFacility.id).equals(facilityId);
        expect(createdFacility.buildingId).equals(building.id);
        expect(createdFacility.buildingName).equals(building.name);
        expect(createdFacility.subRegionName).equals(building.subRegionName);
        expect(createdFacility.regionName).equals(building.regionName);
        expect(createdFacility.stateName).equals(building.stateName);
        expect(createdFacility.countryName).equals(building.countryName);

        // UPDATE
        const newBuilding = await dbHelper.createTestBuilding();
        const updateRequestBody: IFacilityCreateUpdateParams = {
            id: facilityId.toUpperCase(),
            buildingId: newBuilding.id,
            displayName: createdFacility.displayName + " UPDATED",
            facilityType: createRequestBody.facilityType,
            floor: createRequestBody.floor + " UPDATED",
            room: createRequestBody.room + " UPDATED"
        };
        const updateResponse = await facilityHelper.updateFacility(facilityId, updateRequestBody);
        expect(updateResponse.data).is.not.undefined;
        const updatedFacility = updateResponse.data;

        expect(updatedFacility.id).equals(facilityId);
        expect(updatedFacility.displayName).equals(updateRequestBody.displayName);
        expect(updatedFacility.floor).equals(updateRequestBody.floor);
        expect(updatedFacility.room).equals(updateRequestBody.room);
        expect(updatedFacility.buildingId).equals(newBuilding.id);
        expect(updatedFacility.buildingName).equals(newBuilding.name);
        expect(updatedFacility.subRegionName).equals(newBuilding.subRegionName);
        expect(updatedFacility.regionName).equals(newBuilding.regionName);
        expect(updatedFacility.stateName).equals(newBuilding.stateName);
        expect(updatedFacility.countryName).equals(newBuilding.countryName);

        // GET
        const retrieveResponse = await facilityHelper.getFacility(facilityId);
        expect(retrieveResponse.data).is.not.undefined;
        const retrievedFacility = retrieveResponse.data!;

        expect(retrievedFacility.id).equals(facilityId);
        expect(retrievedFacility.displayName).equals(updateRequestBody.displayName);
        expect(retrievedFacility.floor).equals(updateRequestBody.floor);
        expect(retrievedFacility.room).equals(updateRequestBody.room);
        expect(retrievedFacility.buildingId).equals(newBuilding.id);
        expect(retrievedFacility.buildingName).equals(newBuilding.name);
        expect(retrievedFacility.subRegionName).equals(newBuilding.subRegionName);
        expect(retrievedFacility.regionName).equals(newBuilding.regionName);
        expect(retrievedFacility.stateName).equals(newBuilding.stateName);
        expect(retrievedFacility.countryName).equals(newBuilding.countryName);

        // GET with case change
        await facilityHelper.getFacility(facilityId.toUpperCase());

        // Pause for 30 seconds to avoid 404 from Graph API
        await new Promise(resolve => setTimeout(resolve, 30000));

        // DELETE
        await facilityHelper.deleteFacility(createdFacility.id);
    });

    it("Facility - Create should create the correct MS channels", async function (): Promise<void> {
        // CREATE facility
        const createRequestBody = await facilityHelper.createFacilityRequestBody();
        const createResponse = await facilityHelper.createFacility(createRequestBody);
        expect(createResponse.data).is.not.undefined;
        const createdFacility = createResponse.data;

        // GET tenant settings channels
        const channels = await TenantSettingsHelper.getChannels();
        const facilityChannels = channels.filter(f => f.type === "facility");

        // Match facility channels against tenant settings channels
        const facility = (await dbHelper.getFacility(createdFacility.id))!;
        facilityChannels.forEach((channel) => {
            const idx = facility.team.channels.map(c => c.name).indexOf(channel.name);
            expect(idx, `Channel name '${channel.name}' is not found`).is.greaterThan(-1);
        });
    });

    it("Facility - Retrieve team ID by channel ID", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility();

        // Get team ID by channel ID
        const channel = facility.team.channels[0];
        const retrieveResponse = await facilityHelper.getTeamIdByChannelId(channel.msChannelId);

        expect(retrieveResponse.data).is.not.undefined;
        expect(retrieveResponse.data).to.equal(facility.team.msTeamId);
    });

    describe("Facility Exceptions", async function (): Promise<void> {
        it("Facility - Get - 404 Unknown Facility", async function (): Promise<void> {
            await facilityHelper.getFacility("UNKNOWN_FACILITY_ID", 404);
        });

        it("Facility - Create - 409 facility already exists", async function (): Promise<void> {
            // CREATE facility
            const existingFacility = await dbHelper.createTestFacility();

            // CREATE facility with same id
            const createRequest = await facilityHelper.createFacilityRequestBody({ facilityId: existingFacility.id });
            await facilityHelper.createFacility(createRequest, 409);
        });

        it("Facility - Create - 404 building not found", async function (): Promise<void> {
            const createRequest = await facilityHelper.createFacilityRequestBody(<any>{ building: { id: "UNKNOWN_BUILDING_ID" } });
            await facilityHelper.createFacility(createRequest, 404);
        });

        it("Facility - Update - 400 Mismatched Facility", async function (): Promise<void> {
            const updateRequest = await facilityHelper.createFacilityRequestBody();
            await facilityHelper.updateFacility("MISMATCHED_FACILITY_ID", updateRequest, 400);
        });

        it("Facility - Update - 404 Unknown Building", async function (): Promise<void> {
            const updateRequest = await facilityHelper.createFacilityRequestBody();
            updateRequest.buildingId = "UNKNOWN_BUILDING_ID";
            await facilityHelper.updateFacility(updateRequest.id, updateRequest, 404);
        });

        it("Facility - Update - 404 Unknown Facility", async function (): Promise<void> {
            const updateRequest = await facilityHelper.createFacilityRequestBody();
            await facilityHelper.updateFacility(updateRequest.id, updateRequest, 404);
        });

        it("Facility - Delete - 404 Unknown Facility", async function (): Promise<void> {
            await facilityHelper.deleteFacility("UNKNOWN_FACILITY_ID", 404);
        });

        it("Facility - Delete - 409 existing calendar linked", async function (): Promise<void> {
            // CREATE facility
            const facility = await dbHelper.createTestFacility();

            // CREATE calendar
            await dbHelper.createTestCalendar({ facility });

            // DELETE facility
            await facilityHelper.deleteFacility(facility.id, 409);
        });

        it("Facility - Retrieve team ID - 404 Unknown Channel", async function (): Promise<void> {
            await facilityHelper.getTeamIdByChannelId("UNKNOWN_CHANNEL_ID", 404);
        });

    });
});