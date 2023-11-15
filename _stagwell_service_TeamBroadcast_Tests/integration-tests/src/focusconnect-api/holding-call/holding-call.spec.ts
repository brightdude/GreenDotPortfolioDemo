import { expect } from "chai";
import { CosmosDbHelper, ICalendar, IChatMessageCreateParams } from "../../helpers/cosmos-helper";
import { GraphAccessUserType, GraphHelper } from "../../helpers/graph-helper";
import { CalendarHelper } from "../calendar/calendar-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { HoldingCallHelper } from "./holding-call-helper";

const holdingCallHelper = new HoldingCallHelper();
const calendarHelper = new CalendarHelper();
const dbHelper = new CosmosDbHelper();

describe("Holding Call", async function (): Promise<void> {

  before(async function () {
    console.log("Holding Call Before...");
  });

  after(async function () {
    console.log("Holding Call After...");
    //await dbHelper.deleteTestData(false, ["Departments", "PersonnelRoles", "Titles", "Locations"]);
  });

  this.timeout(360000);

  it("Holding Call - Retrieve all", async function (): Promise<void> {
    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar();

    // CREATE holding calls
    const holdingCall1 = await dbHelper.createTestHoldingCall({ calendar });
    const holdingCall2 = await dbHelper.createTestHoldingCall({ calendar });

    // GET all holding calls
    let holdingCallsResponse = await holdingCallHelper.getHoldingCalls(true);
    expect(holdingCallsResponse.data.length).to.be.greaterThan(1);
    let calendarsFiltered = holdingCallsResponse.data.filter(c => c.externalCalendarId === calendar.externalCalendarId);
    expect(calendarsFiltered).to.have.lengthOf(2);
    let meetingIds = calendarsFiltered.map(c => c.holdingCall.msMeetingId);
    expect(meetingIds).to.include(holdingCall1.msMeetingId);
    expect(meetingIds).to.include(holdingCall2.msMeetingId);

    // GET unexpired holding calls
    holdingCallsResponse = await holdingCallHelper.getHoldingCalls(false);
    expect(holdingCallsResponse.data.length).to.be.greaterThan(0);
    calendarsFiltered = holdingCallsResponse.data.filter(c => c.externalCalendarId === calendar.externalCalendarId);
    expect(calendarsFiltered).to.have.lengthOf(1);
    meetingIds = calendarsFiltered.map(c => c.holdingCall.msMeetingId);
    expect(meetingIds).to.not.include(holdingCall1.msMeetingId);
    expect(meetingIds).to.include(holdingCall2.msMeetingId);
  });

  it("Holding Call - Retrieve by Calendar", async function (): Promise<void> {
    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar();

    // CREATE holding calls
    const holdingCall1 = await dbHelper.createTestHoldingCall({ calendar });
    const holdingCall2 = await dbHelper.createTestHoldingCall({ calendar });

    // GET all holding calls
    let holdingCallsResponse = await holdingCallHelper.getHoldingCallsForCalendar(calendar.externalCalendarId, true);
    expect(holdingCallsResponse.data).to.have.lengthOf(1);
    expect(holdingCallsResponse.data[0].holdingCalls).to.have.lengthOf(2);

    // GET unexpired holding calls
    holdingCallsResponse = await holdingCallHelper.getHoldingCallsForCalendar(calendar.externalCalendarId, false);
    expect(holdingCallsResponse.data).to.have.lengthOf(1);
    expect(holdingCallsResponse.data[0].holdingCalls).to.have.lengthOf(1);

    const calendarsFiltered = holdingCallsResponse.data.filter(hc => hc.externalCalendarId == calendar.externalCalendarId);
    expect(calendarsFiltered.length).equals(1);
    const holdingCallsFiltered = calendarsFiltered.map(c => c.holdingCalls).flat().filter(hc => !hc.isExpired);
    expect(holdingCallsFiltered.length).equals(1);

    const retrievedHoldingCall = holdingCallsFiltered[0];
    expect(retrievedHoldingCall.msMeetingId).equals(holdingCall2.msMeetingId);
    expect(retrievedHoldingCall.msThreadId).equals(holdingCall2.msThreadId);
    expect(new Date(retrievedHoldingCall.startTime).getTime()).equals(new Date(holdingCall2.startTime).getTime());
    expect(new Date(retrievedHoldingCall.endTime).getTime()).equals(new Date(holdingCall2.endTime).getTime());
    expect(retrievedHoldingCall.msJoinInfo.telephone.fullNumber).equals(holdingCall2.msJoinInfo.telephone.fullNumber);
  });

  it("Holding Call - Retrieve by Team", async function (): Promise<void> {
    // CREATE facility
    const facility = await dbHelper.createTestFacility();

    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar({ facility });

    // CREATE holding call
    const holdingCall = await dbHelper.createTestHoldingCall({ calendar });

    // GET holding call
    const holdingCallsResponse = await holdingCallHelper.getHoldingCallsForTeam(facility.team.msTeamId, true);

    expect(holdingCallsResponse.data).to.have.lengthOf(1);
    const calendarsFiltered = (holdingCallsResponse.data).filter(hc => hc.externalCalendarId == calendar.externalCalendarId);
    expect(calendarsFiltered.length).equals(1);
    const holdingCallsFiltered = calendarsFiltered.map(c => c.holdingCalls).flat().filter(hc => !hc.isExpired);
    expect(holdingCallsFiltered.length).equals(1);

    const retrievedHoldingCall = holdingCallsFiltered[0];
    expect(retrievedHoldingCall.msMeetingId).equals(holdingCall.msMeetingId);
    expect(retrievedHoldingCall.msThreadId).equals(holdingCall.msThreadId);
    expect(new Date(retrievedHoldingCall.startTime).getTime()).equals(new Date(holdingCall.startTime).getTime());
    expect(new Date(retrievedHoldingCall.endTime).getTime()).equals(new Date(holdingCall.endTime).getTime());
    expect(retrievedHoldingCall.msJoinInfo.telephone.fullNumber).equals(holdingCall.msJoinInfo.telephone.fullNumber);
  });

  it("Holding Calls - Retrieve by Team Multiple calendars", async (): Promise<void> => {
    const f1 = await dbHelper.createTestFacility();
    const calRequest = {
      facility: f1,
      focusUsers: [],
      recorders: [],
    }
    const createdCalendars = await Promise.all([
      dbHelper.createTestCalendar(calRequest), 
      dbHelper.createTestCalendar(calRequest)
    ]);
    await Promise.all([
      dbHelper.createTestHoldingCall({ calendar: createdCalendars[0]}),
      dbHelper.createTestHoldingCall({ calendar: createdCalendars[1]})
    ]);

    const holdingCalls = (await holdingCallHelper.getHoldingCallsForTeam(f1.team.msTeamId,true)).data;

    expect(holdingCalls.length).equal(2);
  });

  it("Holding Call - Create", async function (): Promise<void> {
    const calendar = await dbHelper.createTestCalendar();
    const holdingCallCreateParams = await holdingCallHelper.createHoldingCallCreateRequestBody({ calendar });
    const createResponse = await holdingCallHelper.createHoldingCall(holdingCallCreateParams.externalCalendarId, holdingCallCreateParams);
    expect(createResponse.data.isExpired).equals(false);
    expect(new Date(createResponse.data.startTime).getTime()).equals(new Date(holdingCallCreateParams.startTime).getTime());
    expect(new Date(createResponse.data.endTime).getTime()).equals(new Date(holdingCallCreateParams.endTime).getTime());
  });

  it("Holding Call - Delete", async function (): Promise<void> {
    // CREATE
    const calendar = await dbHelper.createTestCalendar();
    const holdingCall = await dbHelper.createTestHoldingCall({ calendar, createMeeting: true });

    // GET holding call meeting
    const graphHelper = new GraphHelper();
    const meeting = (await graphHelper.OnlineMeetingGet(GraphAccessUserType.WaitingRoomService, holdingCall.msMeetingId))!;
    expect(meeting.subject).includes(calendar.externalCalendarId);

    // DELETE   
    await holdingCallHelper.deleteHoldingCall(calendar.externalCalendarId);

    // GET holding call meeting again
    await graphHelper.OnlineMeetingGet(GraphAccessUserType.WaitingRoomService, holdingCall.msMeetingId, 404);
  });

  it("Holding Call - Delete via calendar delete", async function (): Promise<void> {
    // CREATE facility
    const facility = await dbHelper.createTestFacility({ createTeam: true });

    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar({ facility });

    // CREATE holding call
    const holdingCall = await dbHelper.createTestHoldingCall({ calendar, createMeeting: true });

    // GET holding call meeting
    const graphHelper = new GraphHelper();
    const meeting = (await graphHelper.OnlineMeetingGet(GraphAccessUserType.WaitingRoomService, holdingCall.msMeetingId))!;
    expect(meeting.subject).includes(calendar.externalCalendarId);

    // GET holding calls for calendar
    const getResponse = await holdingCallHelper.getHoldingCallsForCalendar(calendar.externalCalendarId, true);
    const calendarHoldingCalls = getResponse.data;
    expect(calendarHoldingCalls.length).greaterThan(0);
    expect(calendarHoldingCalls[0].holdingCalls.length).greaterThan(0);

    // DELETE calendar
    await calendarHelper.deleteCalendar(calendar.externalCalendarId);

    // GET holding calls for calendar again
    await holdingCallHelper.getHoldingCallsForCalendar(calendar.externalCalendarId, true, 404);

    // GET holding call meeting again
    await graphHelper.OnlineMeetingGet(GraphAccessUserType.WaitingRoomService, holdingCall.msMeetingId, 404);
  });

  it("Holding Call - Create retrieve and delete", async function (): Promise<void> {
    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar();

    // CREATE holding call
    const holdingCallCreateParams = await holdingCallHelper.createHoldingCallCreateRequestBody({ calendar });
    await holdingCallHelper.createHoldingCall(calendar.externalCalendarId, holdingCallCreateParams);

    // GET holding calls
    const getResponse1 = await holdingCallHelper.getHoldingCallsForCalendar(holdingCallCreateParams.externalCalendarId, true);
    const calendarHoldingCalls = getResponse1.data;
    expect(calendarHoldingCalls.length).equals(1);
    const holdingCalls1 = calendarHoldingCalls[0].holdingCalls;
    expect(holdingCalls1.length).greaterThan(0);
    expect(holdingCalls1.filter(hc => !hc.isExpired).length).greaterThan(0);

    // CREATE department
    const newDepartment = await dbHelper.createTestLookup("Departments", "department");

    // UPDATE calendar
    const updateResponse = await calendarHelper.updateCalendar(calendar.externalCalendarId, { externalCalendarId: calendar.externalCalendarId, departmentId: newDepartment.id });
    expect(updateResponse.data).is.not.undefined;
    const updatedCalendar = updateResponse.data;
    expect(updatedCalendar.externalCalendarId).equals(calendar.externalCalendarId);
    expect(updatedCalendar.departmentId).equals(newDepartment.id);
    expect(updatedCalendar.departmentName).equals(newDepartment.name);

    // Check the holding call still exists
    const getResponse2 = await holdingCallHelper.getHoldingCallsForCalendar(calendar.externalCalendarId, true);
    expect(getResponse2.data).is.not.undefined;
    const holdingCalls2 = getResponse2.data;

    const calendarsFiltered = holdingCalls2.filter(hc => hc.externalCalendarId == calendar.externalCalendarId);
    expect(calendarsFiltered.length).greaterThan(0);
    const holdingCallsFiltered = calendarsFiltered.map(c => c.holdingCalls).flat().filter(hc => !hc.isExpired);
    expect(holdingCallsFiltered.length).greaterThan(0);

    const facility = (await dbHelper.getFacility(calendar.facilityId!))!;
    expect(facility).is.not.undefined;

    // List holding calls members
    const meetingMembersResponse = await holdingCallHelper.getMembersForTeam(facility.team.msTeamId);
    expect(meetingMembersResponse.data).to.have.lengthOf(1);
    const members = meetingMembersResponse.data[0].members;
    expect(members).to.have.lengthOf(0);

    // CREATE chat message
    const meetingId = holdingCallsFiltered[0].msMeetingId;
    const messageContent = `Message content for team ID ${facility.team.msTeamId} and meeting ID ${meetingId}`;
    const messageResponse = await holdingCallHelper.createHoldingCallMessage(facility.team.msTeamId, meetingId, {
      teamId: facility.team.msTeamId,
      meetingId,
      content: messageContent
    });

    expect(messageResponse.data.body.content).to.equal(messageContent);

    const holdingCall2Input = await holdingCallHelper.createHoldingCallCreateRequestBody({ calendar });
    await holdingCallHelper.createHoldingCall(holdingCall2Input.externalCalendarId, holdingCall2Input);

    // DELETE   
    await holdingCallHelper.deleteHoldingCall(holdingCallCreateParams.externalCalendarId);

    const getResponse3 = await holdingCallHelper.getHoldingCallsForCalendar(holdingCallCreateParams.externalCalendarId, true);
    expect(getResponse3.data.length).greaterThan(0);
    const holdingCalls3 = getResponse3.data[0].holdingCalls;
    expect(holdingCalls3.length).equals(2);
    expect(holdingCalls3.filter(hc => !hc.isExpired).length).equals(0);
  });

  describe("Holding Call Exceptions", async function (): Promise<void> {
    it("Holding Call - Retrieve by Calendar - 404 calendar not found", async function (): Promise<void> {
      await holdingCallHelper.getHoldingCallsForCalendar("UNKNOWN_CALENDAR_ID", true, 404);
    });

    it("Holding Call - Retrieve by Team - 404 team not found", async function (): Promise<void> {
      await holdingCallHelper.getHoldingCallsForTeam("UNKNOWN_TEAM_ID", true, 404);
    });

    it("Holding Call - Create - 404 calendar not found", async function (): Promise<void> {
      const calendar: ICalendar = {
        id: LookupHelper.createTestGuid(),
        externalCalendarId: LookupHelper.createTestGuid(),
        departmentId: LookupHelper.createTestGuid(),
        departmentName: "Test dept"
      };
      const holdingCallCreateParams = await holdingCallHelper.createHoldingCallCreateRequestBody({ calendar });
      await holdingCallHelper.createHoldingCall(calendar.externalCalendarId, holdingCallCreateParams, 404);
    });

    it("Holding Call - Create - 400 start time in past", async function (): Promise<void> {
      const calendar = await dbHelper.createTestCalendar();
      const holdingCallCreateParams = await holdingCallHelper.createHoldingCallCreateRequestBody({ calendar, createInPast: true });
      await holdingCallHelper.createHoldingCall(calendar.externalCalendarId, holdingCallCreateParams, 400);
    });

    it("Holding Call - Create - 400 end time before start time", async function (): Promise<void> {
      const calendar = await dbHelper.createTestCalendar();
      const holdingCallCreateParams = await holdingCallHelper.createHoldingCallCreateRequestBody({ calendar });
      let endTime = new Date(holdingCallCreateParams.startTime);
      endTime.setFullYear(endTime.getFullYear() - 1);
      holdingCallCreateParams.endTime = endTime.toISOString();
      await holdingCallHelper.createHoldingCall(calendar.externalCalendarId, holdingCallCreateParams, 400);
    });

    it("Holding Call - Delete - 404 calendar not found", async function (): Promise<void> {
      await holdingCallHelper.deleteHoldingCall("UNKNOWN_CALENDAR_ID", 404);
    });

    it("Holding Call - Create chat message - 404 meeting not found", async function (): Promise<void> {
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
      await holdingCallHelper.createHoldingCallMessage(facility.team.msTeamId, meetingId, createOnDemandMeetingMessageCreateParams, 404);
    });

    it("Holding Call - Create chat message - 400 chat message cannot be blank", async function (): Promise<void> {
      const teamId = LookupHelper.createTestGuid();
      const meetingId = LookupHelper.createTestGuid();
      const createOnDemandMeetingMessageCreateParams: IChatMessageCreateParams = {
        teamId,
        meetingId,
        content: ""
      }
      await holdingCallHelper.createHoldingCallMessage(teamId, meetingId, createOnDemandMeetingMessageCreateParams, 400);
    });
  });
});