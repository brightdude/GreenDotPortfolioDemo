import { expect } from "chai";
import moment from "moment";
import { CosmosDbHelper, IOnDemandMeetingCreateParams, IChatMessageCreateParams } from "../../helpers/cosmos-helper";
import { GraphAccessUserType, GraphHelper } from "../../helpers/graph-helper";
import { FacilityHelper } from "../facility/facility-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { OnDemandMeetingHelper } from "./on-demand-meeting-helper";

const onDemandMeetingHelper = new OnDemandMeetingHelper();
const facilityHelper = new FacilityHelper();
const dbHelper = new CosmosDbHelper();

describe("OnDemandMeeting", async function (): Promise<void> {

    before(async function () {
        console.log("OnDemandMeeting Before...");
    });

    after(async function () {
        console.log("OnDemandMeeting After...");
        //await dbHelper.deleteTestData(false, ["Departments", "PersonnelRoles", "Titles", "Locations"]);
    });

    this.timeout(360000);

    it("OnDemandMeeting - Retrieve all", async function (): Promise<void> {
        const onDemandMeeting = await dbHelper.createTestOnDemandMeeting();
        const getResponse = await onDemandMeetingHelper.retrieveOnDemandMeetingList(onDemandMeeting.msTeamId);
        expect(getResponse.data.length).equals(1);
        expect(getResponse.data[0].id).equals(onDemandMeeting.id);
    });

    it("OnDemandMeeting - Create", async function (): Promise<void> {
        // CREATE facility with team
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE user
        const user = await dbHelper.createTestUser();

        // CREATE on-demand meeting
        const now = moment();
        let month = now;
        month = month.add(1, "month");
        const startTime = moment(month).toISOString();
        const endTime = month.add(1, "hour").toISOString();
        const params: IOnDemandMeetingCreateParams = {
            teamId: facility.team.msTeamId,
            meetingName: `__test Meeting ${(Math.random() * 999).toFixed(0).padStart(3, "0")}`,
            organizer: user.email,
            startDateTime: startTime,
            endDateTime: endTime
        };
        const createResponse = await onDemandMeetingHelper.createOnDemandMeeting(facility.team.msTeamId, params);
        const createdOnDemandMeeting = createResponse.data;

        // GET on-demand meeting
        const onDemandMeeting = await dbHelper.getOnDemandMeeting(facility.team.msTeamId);
        expect(onDemandMeeting).to.not.be.empty;
        expect(onDemandMeeting.id).equals(createdOnDemandMeeting.id);
        expect(onDemandMeeting.msTeamId).equals(createdOnDemandMeeting.msTeamId);
        expect(onDemandMeeting.activeFlag).equals(true);
        expect(onDemandMeeting.organizer).equals(user.email);
        expect(new Date(onDemandMeeting.startDateTime).getTime()).equals(new Date(startTime).getTime());
        expect(new Date(onDemandMeeting.endDateTime).getTime()).equals(new Date(endTime).getTime());

        // GET graph online meeting
        const graphHelper = new GraphHelper();
        const graphMeeting = await graphHelper.OnlineMeetingGet(GraphAccessUserType.OnDemandService, onDemandMeeting.msMeetingId);
        expect(graphMeeting).is.not.undefined;
        expect(graphMeeting?.id).equals(onDemandMeeting.msMeetingId);
        expect(graphMeeting?.subject).equals(onDemandMeeting.meetingName);
        expect(new Date(graphMeeting!.startDateTime).getTime()).equals(new Date(onDemandMeeting.startDateTime).getTime());
        expect(new Date(graphMeeting!.endDateTime).getTime()).equals(new Date(onDemandMeeting.endDateTime).getTime());
    });

    it("OnDemandMeeting - Delete", async function (): Promise<void> {
        // CREATE facility with team
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE on-demand meeting
        const meeting = await dbHelper.createTestOnDemandMeeting(facility);

        // DELETE on-demand meeting
        await onDemandMeetingHelper.deleteOnDemandMeeting(meeting.msTeamId, meeting.id);

        // GET on-demand meeting
        const deletedMeeting = await dbHelper.getOnDemandMeeting(meeting.msTeamId);
        expect(deletedMeeting.activeFlag).is.false;

        // GET graph online meeting
        const graphHelper = new GraphHelper();
        const graphMeeting = await graphHelper.OnlineMeetingGet(GraphAccessUserType.OnDemandService, meeting.msMeetingId, 404);
        expect(graphMeeting).is.undefined;
    });

    it("OnDemandMeeting - Create chat message", async function (): Promise<void> {
        // CREATE facility with team
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE on-demand meeting
        const meeting = await dbHelper.createTestOnDemandMeeting(facility);

        // CREATE chat message
        const content = `Message content for team ID ${facility.team.msTeamId} and meeting ID ${meeting.id}`;
        const createOnDemandMeetingMessageCreateParams: IChatMessageCreateParams = {
            teamId: facility.team.msTeamId,
            meetingId: meeting.id,
            content
        }
        const createMessageResponse = await onDemandMeetingHelper.createOnDemandMeetingMessage(facility.team.msTeamId, meeting.id, createOnDemandMeetingMessageCreateParams);
        expect(createMessageResponse.data.body.content).equals(content);
    });

    it("OnDemandMeeting - Create, Get, Send message, Delete", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE organizer user
        const organizer = await dbHelper.createTestUser();

        // CREATE on-demand meeting
        const now = moment();
        let month = now;
        month = month.add(1, "month");
        const startTime = moment(month).toISOString();
        const endTime = month.add(1, "hour").toISOString();
        const params: IOnDemandMeetingCreateParams = {
            teamId: facility.team.msTeamId,
            meetingName: `__test Meeting ${(Math.random() * 999).toFixed(0).padStart(3, "0")}`,
            organizer: organizer.email,
            startDateTime: startTime,
            endDateTime: endTime
        };
        const createResponse = await onDemandMeetingHelper.createOnDemandMeeting(facility.team.msTeamId, params);
        const createdOnDemandMeeting = createResponse.data;
        expect(createdOnDemandMeeting).to.not.be.empty;

        // GET on-demand meetings
        const retrieveResponse = await onDemandMeetingHelper.retrieveOnDemandMeetingList(facility.team.msTeamId);
        const onDemandMeetingList = retrieveResponse.data;
        expect(onDemandMeetingList.length).to.equal(1);
        expect(onDemandMeetingList[0].id).to.equal(createdOnDemandMeeting.id);
        expect(onDemandMeetingList[0].facilityId).to.equal(facility.id);
        expect(onDemandMeetingList[0].organizer).to.equal(organizer.email);

        // List on-demand meetings members
        const meetingMembersResponse = await onDemandMeetingHelper.getMembersForTeam(facility.team.msTeamId);
        expect(meetingMembersResponse.data).to.have.lengthOf(1);
        const members = meetingMembersResponse.data[0].members;
        expect(members).to.have.lengthOf(0);

        // Send message to on-demand meeting
        const createOnDemandMeetingMessageCreateParams: IChatMessageCreateParams = {
            teamId: facility.team.msTeamId,
            meetingId: createdOnDemandMeeting.id,
            content: `Message content for team ID ${facility.team.msTeamId} and meeting ID ${createdOnDemandMeeting.id}`
        }
        await onDemandMeetingHelper.createOnDemandMeetingMessage(facility.team.msTeamId, createdOnDemandMeeting.id, createOnDemandMeetingMessageCreateParams);

        // GET facility (should include on-demand meeting count)
        const getFacilityResponse = await facilityHelper.getFacility(facility.id);
        expect(getFacilityResponse.data).is.not.undefined;
        const facility2 = getFacilityResponse.data!;
        expect(facility2.activeOnDemandMeetingCount).equals(1);

        // DELETE on-demand meeting
        await onDemandMeetingHelper.deleteOnDemandMeeting(facility.team.msTeamId, createdOnDemandMeeting.id);
    });

    describe("OnDemandMeeting Exceptions", async function (): Promise<void> {
        it("OnDemandMeeting - Create - 404 unknown team ID", async function (): Promise<void> {
            const teamId = "INVALID_TEAM_ID";
            const now = moment();
            let month = now;
            month = month.add(1, "month");
            const startTime = moment(month).toISOString();
            const endTime = month.add(1, "hour").toISOString();
            const params: IOnDemandMeetingCreateParams = {
                teamId,
                meetingName: `__test Meeting ${(Math.random() * 999).toFixed(0).padStart(3, "0")}`,
                organizer: "meeting-organizer@contoso.com",
                startDateTime: startTime,
                endDateTime: endTime
            }
            await onDemandMeetingHelper.createOnDemandMeeting(teamId, params, 404);
        });

        it("OnDemandMeeting - Create - 400 start time in past", async function (): Promise<void> {
            // CREATE facility
            const facility = await dbHelper.createTestFacility();

            // CREATE on-demand meeting
            const now = moment();
            let month = now;
            month = month.add(-1, "month");
            const startTime = moment(month).toISOString();
            const endTime = month.add(1, "hour").toISOString();
            const params: IOnDemandMeetingCreateParams = {
                teamId: facility.team.msTeamId,
                meetingName: `__test Meeting ${(Math.random() * 999).toFixed(0).padStart(3, "0")}`,
                organizer: "meeting-organizer@contoso.com",
                startDateTime: startTime,
                endDateTime: endTime
            }
            await onDemandMeetingHelper.createOnDemandMeeting(facility.team.msTeamId, params, 400);
        });

        it("OnDemandMeeting - Create - 400 end time before start time", async function (): Promise<void> {
            // CREATE facility
            const facility = await dbHelper.createTestFacility();

            // CREATE on-demand meeting
            const now = moment();
            let month = now;
            month = month.add(1, "month");
            const startTime = moment(month).toISOString();
            const endTime = month.add(-1, "hour").toISOString();
            const params: IOnDemandMeetingCreateParams = {
                teamId: facility.team.msTeamId,
                meetingName: `__test Meeting ${(Math.random() * 999).toFixed(0).padStart(3, "0")}`,
                organizer: "meeting-organizer@contoso.com",
                startDateTime: startTime,
                endDateTime: endTime
            }
            await onDemandMeetingHelper.createOnDemandMeeting(facility.team.msTeamId, params, 400);
        });

        it("OnDemandMeeting - Delete - 404 meeting not found", async function (): Promise<void> {
            await onDemandMeetingHelper.deleteOnDemandMeeting("UNKNOWN_TEAM_ID", "UNKNOWN_MEETING_ID", 404);
        });

        it("OnDemandMeeting - Create chat message - 404 meeting not found", async function (): Promise<void> {
            // CREATE facility
            const facility = await dbHelper.createTestFacility();

            // CREATE chat message
            const meetingId = "UNKNOWN_MEETING_ID";
            const content = `Message content for team ID ${facility.team.msTeamId} and meeting ID ${meetingId}`;
            const createOnDemandMeetingMessageCreateParams: IChatMessageCreateParams = {
                teamId: facility.team.msTeamId,
                meetingId,
                content
            }
            await onDemandMeetingHelper.createOnDemandMeetingMessage(facility.team.msTeamId, meetingId, createOnDemandMeetingMessageCreateParams, 404);
        });

        it("OnDemandMeeting - Create chat message - 400 chat message cannot be blank", async function (): Promise<void> {
            const teamId = LookupHelper.createTestGuid();
            const meetingId = LookupHelper.createTestGuid();
            const createOnDemandMeetingMessageCreateParams: IChatMessageCreateParams = {
                teamId,
                meetingId,
                content: ""
            }
            await onDemandMeetingHelper.createOnDemandMeetingMessage(teamId, meetingId, createOnDemandMeetingMessageCreateParams, 400);
        });
    });
});