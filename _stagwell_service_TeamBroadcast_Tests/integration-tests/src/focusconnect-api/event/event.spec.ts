import { expect } from "chai";
import { CosmosDbHelper, IEventCreateUpdateParams } from "../../helpers/cosmos-helper";
import { GraphAccessUserType, GraphHelper } from "../../helpers/graph-helper";
import { CalendarHelper } from "../calendar/calendar-helper";
import { FacilityHelper } from "../facility/facility-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { EventHelper } from "./event-helper";

const eventHelper = new EventHelper();
const dbHelper = new CosmosDbHelper();

describe("Event", async function (): Promise<void> {

  before(async function () {
    console.log("Event Before...");
  });

  after(async function () {
    console.log("Event After...");
    //await dbHelper.deleteTestData(false, ["Departments", "PersonnelRoles", "Titles", "Locations"]);
  });

  this.timeout(360000);

  it("Event - Retrieve all active events", async function (): Promise<void> {
    const event1 = await dbHelper.createTestEvent();
    const event2 = await dbHelper.createTestEvent();

    const getResponse1 = await eventHelper.getEvents();
    const eventIdList1 = getResponse1.data.map(u => u.id);
    expect(eventIdList1).to.contain(event1.id);
    expect(eventIdList1).to.contain(event2.id);

    event2.status = "Deleted";
    await dbHelper.updateEvent(event2);

    const getResponse2 = await eventHelper.getEvents();
    const eventIdList2 = getResponse2.data.map(u => u.id);
    expect(eventIdList2).to.contain(event1.id);
    expect(eventIdList2).to.not.contain(event2.id);
  });

  it("Event - Retrieve all active events for a team", async function (): Promise<void> {
    const facility = await dbHelper.createTestFacility();
    const calendar1 = await dbHelper.createTestCalendar({ facility });
    const event11 = await dbHelper.createTestEvent({ calendar: calendar1 });
    const event12 = await dbHelper.createTestEvent({ calendar: calendar1 });
    const calendar2 = await dbHelper.createTestCalendar({ facility });
    const event21 = await dbHelper.createTestEvent({ calendar: calendar2 });
    const event22 = await dbHelper.createTestEvent({ calendar: calendar2 });

    const getResponse1 = await eventHelper.getEventsForTeam(facility.team.msTeamId);
    const eventIdList1 = getResponse1.data.map(u => u.id);
    expect(eventIdList1).to.contain(event11.id);
    expect(eventIdList1).to.contain(event12.id);
    expect(eventIdList1).to.contain(event21.id);
    expect(eventIdList1).to.contain(event22.id);

    event12.status = "Deleted";
    await dbHelper.updateEvent(event12);
    event22.status = "Deleted";
    await dbHelper.updateEvent(event22);

    const getResponse2 = await eventHelper.getEventsForTeam(facility.team.msTeamId);
    const eventIdList2 = getResponse2.data.map(u => u.id);
    expect(eventIdList2).to.contain(event11.id);
    expect(eventIdList2).to.not.contain(event12.id);
    expect(eventIdList2).to.contain(event21.id);
    expect(eventIdList2).to.not.contain(event22.id);
  });

  it("Event - Retrieve", async function (): Promise<void> {
    // CREATE facility
    const facility = await dbHelper.createTestFacility();

    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar({ facility });

    // CREATE event
    const externalIdLower = "ext" + (Math.random() * 999).toFixed(0).padStart(3, "0");
    const event = await dbHelper.createTestEvent({ calendar, externalId: externalIdLower });

    // RETRIEVE event
    const getResponse = await eventHelper.getEvent(externalIdLower.toUpperCase());
    const retrievedEvent = getResponse.data!;
    
    expect(retrievedEvent.id).equals(event.id);
    expect(retrievedEvent.externalId).equals(event.externalId);
    expect(retrievedEvent.facilityId).equals(facility.id);
    expect(retrievedEvent.calendarId).equals(calendar.id);
    expect(retrievedEvent.msTeamId).equals(facility.team.msTeamId);
  });

  it("Event - Create", async function (): Promise<void> {
    // CREATE facility
    const facility = await dbHelper.createTestFacility({ createTeam: true });

    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar({ facility });

    // CREATE event
    const externalId = (Math.random() * 999).toFixed(0).padStart(3, "0");
    const req = eventHelper.createTestEventRequestBody(externalId, calendar.externalCalendarId);
    const createEventResponse = await eventHelper.createEvent(externalId, req);

    expect(createEventResponse.data.externalId).equals(externalId);
    expect(createEventResponse.data.facilityId).equals(facility.id);
    expect(createEventResponse.data.calendarId).equals(calendar.id);
    expect(createEventResponse.data.externalCalendarId).equals(calendar.externalCalendarId);
    expect(new Date(createEventResponse.data.startTime).getTime()).equals(new Date(req.startTime).getTime());
    expect(new Date(createEventResponse.data.endTime).getTime()).equals(new Date(req.endTime).getTime());
    expect(createEventResponse.data.status).equals("Active");
  });

  it("Event - Update", async function (): Promise<void> {
    // CREATE facility
    const facility = await dbHelper.createTestFacility({ createTeam: true });

    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar({ facility });

    // CREATE event
    const externalIdLower = "ext" + (Math.random() * 999).toFixed(0).padStart(3, "0");
    const event = await dbHelper.createTestEvent({ calendar, externalId: externalIdLower, createMeeting: true });

    // UPDATE event
    const startTime = new Date(event.startTime);
    startTime.setHours(startTime.getHours() - 1);
    const endTime = new Date(event.endTime);
    endTime.setHours(endTime.getHours() + 1);
    const eventUpdateRequest: IEventCreateUpdateParams = {
      externalCalendarId: calendar.externalCalendarId,
      startTime: startTime.toISOString(),
      endTime: endTime.toISOString(),
      subject: event.subject + " UPDATED"
    };
    const updateEventResponse = await eventHelper.updateEvent(externalIdLower.toUpperCase(), eventUpdateRequest);

    expect(updateEventResponse.data.id).equals(event.id);
    expect(updateEventResponse.data.externalId).equals(event.externalId);
    expect(updateEventResponse.data.facilityId).equals(facility.id);
    expect(updateEventResponse.data.calendarId).equals(calendar.id);
    expect(updateEventResponse.data.externalCalendarId).equals(calendar.externalCalendarId);
    expect(updateEventResponse.data.caseId).equals(event.caseId);
    expect(new Date(updateEventResponse.data.startTime).getTime()).equals(startTime.getTime());
    expect(new Date(updateEventResponse.data.endTime).getTime()).equals(endTime.getTime());
    expect(updateEventResponse.data.subject).equals(eventUpdateRequest.subject);
    expect(updateEventResponse.data.status).equals("Active");
  });

  it("Event - Delete", async function (): Promise<void> {
    // CREATE facility
    const facility = await dbHelper.createTestFacility({ createTeam: true });

    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar({ facility });

    // CREATE event
    const externalIdLower = "ext" + (Math.random() * 999).toFixed(0).padStart(3, "0");
    const event = await dbHelper.createTestEvent({ calendar, externalId: externalIdLower, createMeeting: true });

    // DELETE event
    await eventHelper.deleteEvent(externalIdLower.toUpperCase());
  });

  it("Event - Create chat message", async function (): Promise<void> {
    // CREATE facility
    const facility = await dbHelper.createTestFacility({ createTeam: true });

    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar({ facility });

    // CREATE event
    const event = await dbHelper.createTestEvent({ calendar, createMeeting: true });

    // CREATE chat message
    const meetingId = event.msMeetingId!;
    const teamId = facility.team.msTeamId;
    const content = `Chat message for team '${teamId}' and meeting '${meetingId}'`;
    const createMessageResponse = await eventHelper.createEventMessage(teamId, meetingId, content);
    expect(createMessageResponse.data.body.content).equals(content);
  });

  it("Event - Create retrieve update and delete", async function (): Promise<void> {
    // CREATE facility
    let facility = await dbHelper.createTestFacility({ createTeam: true });

    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar({ facility });

    // CREATE event
    const externalEventId = LookupHelper.createTestGuid();
    const createEventRequest = eventHelper.createTestEventRequestBody(externalEventId, calendar.externalCalendarId);
    const createResponse = await eventHelper.createEvent(externalEventId, createEventRequest);
    const createdEvent = createResponse.data;
    expect(createdEvent.externalId).equals(externalEventId);
    expect(createdEvent.calendarId).equals(calendar.id);
    expect(createdEvent.externalCalendarId).equals(calendar.externalCalendarId);
    expect(createdEvent.facilityId).equals(facility.id);
    expect(createdEvent.status).equals("Active");

    // GET online meeting
    const graphHelper = new GraphHelper();
    let meeting = await graphHelper.OnlineMeetingGet(GraphAccessUserType.ScheduledEventService, createdEvent.msMeetingId!);
    expect(meeting!.subject).equals(createEventRequest.subject);

    // UPDATE facility
    const facilityHelper = new FacilityHelper();
    facility.displayName = facility.displayName + " UPDATED";
    await facilityHelper.updateFacility(facility.id, facility);

    // UPDATE event
    const updatedSubject = `${createEventRequest.subject} UPDATED`;
    const updateEventRequest: IEventCreateUpdateParams = {
      externalCalendarId: calendar.externalCalendarId,
      body: "something new",
      subject: updatedSubject,
      startTime: createdEvent.startTime,
      endTime: createdEvent.endTime
    };
    const updateResponse = await eventHelper.updateEvent(createResponse.data.externalId, updateEventRequest);
    const updatedEvent = updateResponse.data;
    expect(updatedEvent.body).equals(updateEventRequest.body);
    expect(updatedEvent.subject).equals(updatedSubject);
    expect(new Date(updatedEvent.startTime).getTime()).equals(new Date(updateEventRequest.startTime).getTime());
    expect(new Date(updatedEvent.endTime).getTime()).equals(new Date(updateEventRequest.endTime).getTime());

    // GET online meeting again
    meeting = await graphHelper.OnlineMeetingGet(GraphAccessUserType.ScheduledEventService, createdEvent.msMeetingId!);
    expect(meeting!.id).equals(createdEvent.msMeetingId);
    expect(meeting!.subject).equals(updatedSubject);

    // GET event
    const retrieveResponse = await eventHelper.getEvent(createResponse.data.externalId);
    expect(retrieveResponse.data).is.not.undefined;
    const retrievedEvent = retrieveResponse.data!;
    expect(retrievedEvent.body).equals(updateEventRequest.body);
    expect(retrievedEvent.msMeetingId).is.not.undefined;

    facility = (await dbHelper.getFacility(facility.id))!;

    // CREATE chat message
    const meetingId = retrievedEvent.msMeetingId!;
    const chatContent = `Message content for team ID ${facility.team.msTeamId} and meeting ID ${meetingId}`;
    const messageResponse = await eventHelper.createEventMessage(facility.team.msTeamId, meetingId, chatContent);
    const createdMessage = messageResponse.data;
    expect(createdMessage.body.content).to.equal(chatContent);

    // GET chat message
    const retrievedMessage = await graphHelper.ChatMessageGet(GraphAccessUserType.ScheduledEventService, createdMessage.chatId, createdMessage.id);
    expect(retrievedMessage!.body.content).equals(chatContent);
    expect(new Date(retrievedMessage!.createdDateTime).getTime()).equals(new Date(createdMessage.createdDateTime).getTime());

    // DELETE event
    await eventHelper.deleteEvent(createResponse.data.externalId);

    // GET event again
    await eventHelper.getEvent(createResponse.data.externalId, 404);
  });

  it("Event - Delete via calendar delete", async function (): Promise<void> {
    // CREATE facility
    const facility = await dbHelper.createTestFacility({ createTeam: true });

    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar({ facility });

    // CREATE event
    const event = await dbHelper.createTestEvent({ calendar, createMeeting: true });

    // GET online meeting
    const graphHelper = new GraphHelper();
    const meeting = await graphHelper.OnlineMeetingGet(GraphAccessUserType.ScheduledEventService, event.msMeetingId!);
    expect(meeting!.subject).equals(event.subject);

    // DELETE Calendar
    const calendarHelper = new CalendarHelper();
    await calendarHelper.deleteCalendar(calendar.externalCalendarId);

    // GET event
    await eventHelper.getEvent(event.externalId, 404);
    
    // GET online meeting again
    await graphHelper.OnlineMeetingGet(GraphAccessUserType.ScheduledEventService, event.msMeetingId!, 404);
  });

  it("Event - List events and members", async function (): Promise<void> {
    // Create facility
    const facility = await dbHelper.createTestFacility({ createTeam: true });

    // Create calendar
    const calendar = await dbHelper.createTestCalendar({ facility });

    // Create event
    await dbHelper.createTestEvent({ calendar, createMeeting: true });

    // List events
    const events = await eventHelper.getEventsForTeam(facility.team.msTeamId);
    expect(events.data).to.have.lengthOf(1);

    // List members
    const eventMembersResponse = await eventHelper.getMembersForTeam(facility.team.msTeamId);
    expect(eventMembersResponse.data).to.have.lengthOf(1);
    const members = eventMembersResponse.data[0].members;
    expect(members).to.have.lengthOf(0);
  });

  describe("Event Exceptions", async function (): Promise<void> {
    it("Event - Get - 404 unknown external ID", async function (): Promise<void> {
      const externalIdUpper = "EXT" + (Math.random() * 999).toFixed(0).padStart(3, "0");
      await dbHelper.createTestEvent({ externalId: externalIdUpper });
      await eventHelper.getEvent(externalIdUpper.toLowerCase(), 404);
    });

    it("Event - Create - 400 start date in past", async function (): Promise<void> {
      // CREATE facility
      const facility = await dbHelper.createTestFacility();

      // CREATE calendar
      const calendar = await dbHelper.createTestCalendar({ facility });

      // CREATE event
      const externalId = (Math.random() * 999).toFixed(0).padStart(3, "0");
      const req = eventHelper.createTestEventRequestBody(externalId, calendar.externalCalendarId);
      const startTime = new Date(req.startTime);
      startTime.setFullYear(startTime.getFullYear() - 1);
      req.startTime = startTime.toISOString();
      await eventHelper.createEvent(externalId, req, 400);
    });

    it("Event - Create - 400 end date before start date", async function (): Promise<void> {
      // CREATE facility
      const facility = await dbHelper.createTestFacility();

      // CREATE calendar
      const calendar = await dbHelper.createTestCalendar({ facility });

      // CREATE event
      const externalId = (Math.random() * 999).toFixed(0).padStart(3, "0");
      const req = eventHelper.createTestEventRequestBody(externalId, calendar.externalCalendarId);
      const startTime = new Date(req.startTime);
      startTime.setFullYear(startTime.getFullYear() - 1);
      req.endTime = startTime.toISOString();
      await eventHelper.createEvent(externalId, req, 400);
    });

    it("Event - Create - 400 calendar does not exist", async function (): Promise<void> {
      const externalId = (Math.random() * 999).toFixed(0).padStart(3, "0");
      const req = eventHelper.createTestEventRequestBody(externalId, "UNKNOWN_CALENDAR_ID");
      await eventHelper.createEvent(externalId, req, 400);
    });

    it("Event - Create - 409 event already exists", async function (): Promise<void> {
      // CREATE facility
      const facility = await dbHelper.createTestFacility();

      // CREATE calendar
      const calendar = await dbHelper.createTestCalendar({ facility });

      // CREATE first event
      const externalIdLower = "ext" + (Math.random() * 999).toFixed(0).padStart(3, "0");
      await dbHelper.createTestEvent({ calendar, externalId: externalIdLower });

      // CREATE second event
      const externalIdUpper = externalIdLower.toUpperCase();
      const req = eventHelper.createTestEventRequestBody(externalIdLower, calendar.externalCalendarId);
      await eventHelper.createEvent(externalIdUpper, req, 409);
    });

    it("Event - Update - 409 active event duplicated", async function (): Promise<void> {
      const calendar = await dbHelper.createTestCalendar();
  
      // CREATE events
      const externalId = "duplicate_external_id";
      const event1 = await dbHelper.createTestEvent({ calendar, externalId });
      const event2 = await dbHelper.createTestEvent({ calendar, externalId });
     
      const startTime = new Date(event1.startTime);
      startTime.setHours(startTime.getHours() - 1);
      const endTime = new Date(event1.endTime);
      endTime.setHours(endTime.getHours() + 1);
      const eventUpdateRequest: IEventCreateUpdateParams = {
        externalCalendarId: calendar.externalCalendarId,
        startTime: startTime.toISOString(),
        endTime: endTime.toISOString(),
        subject: event1.subject + " UPDATED"
      };
      await eventHelper.updateEvent(externalId, eventUpdateRequest, 409);
    });

    it("Event - Update - 400 start date in past", async function (): Promise<void> {
      // CREATE facility
      const facility = await dbHelper.createTestFacility();

      // CREATE calendar
      const calendar = await dbHelper.createTestCalendar({ facility });

      // CREATE event
      const event = await dbHelper.createTestEvent({ calendar });

      // UPDATE event
      const startTime = new Date(event.startTime);
      startTime.setFullYear(startTime.getFullYear() - 1);
      const endTime = new Date(event.endTime);
      const eventUpdateRequest: IEventCreateUpdateParams = {
        externalCalendarId: calendar.externalCalendarId,
        startTime: startTime.toISOString(),
        endTime: endTime.toISOString(),
        subject: event.subject + " UPDATED"
      };
      await eventHelper.updateEvent(event.externalId, eventUpdateRequest, 400);
    });

    it("Event - Update - 400 end date before start date", async function (): Promise<void> {
      // CREATE facility
      const facility = await dbHelper.createTestFacility();

      // CREATE calendar
      const calendar = await dbHelper.createTestCalendar({ facility });

      // CREATE event
      const event = await dbHelper.createTestEvent({ calendar });

      // UPDATE event
      const startTime = new Date(event.startTime);
      const endTime = new Date(event.startTime);
      endTime.setHours(endTime.getHours() - 10);
      const eventUpdateRequest: IEventCreateUpdateParams = {
        externalCalendarId: calendar.externalCalendarId,
        startTime: startTime.toISOString(),
        endTime: endTime.toISOString(),
        subject: event.subject + " UPDATED"
      };
      await eventHelper.updateEvent(event.externalId, eventUpdateRequest, 400);
    });

    it("Event - Update - 400 calendar does not exist", async function (): Promise<void> {
      // CREATE facility
      const facility = await dbHelper.createTestFacility();

      // CREATE calendar
      const calendar = await dbHelper.createTestCalendar({ facility });

      // CREATE event
      const event = await dbHelper.createTestEvent({ calendar });

      // UPDATE event
      const eventUpdateRequest: IEventCreateUpdateParams = {
        externalCalendarId: "UNKNOWN_CALENDAR_ID",
        startTime: event.startTime,
        endTime: event.endTime,
        subject: event.subject + " UPDATED"
      };
      await eventHelper.updateEvent(event.externalId, eventUpdateRequest, 400);
    });

    it("Event - Update - 404 event does not exist", async function (): Promise<void> {
      // CREATE calendar
      const calendar = await dbHelper.createTestCalendar();

      // CREATE event
      const event = await dbHelper.createTestEvent({ calendar });

      // UPDATE event
      const eventUpdateRequest: IEventCreateUpdateParams = {
        externalCalendarId: calendar.externalCalendarId,
        startTime: event.startTime,
        endTime: event.endTime,
        subject: event.subject + " UPDATED"
      };
      await eventHelper.updateEvent("UNKNOWN_EXTERNAL_ID", eventUpdateRequest, 404);
    });

    it("Event - Delete - 404 unknown external ID", async function (): Promise<void> {
      await eventHelper.deleteEvent("UNKNOWN_EXTERNAL_ID", 404);
    });

    it("Event - Delete - 409 duplicate external id", async function (): Promise<void> {
      // CREATE calendar
      const calendar = await dbHelper.createTestCalendar();
  
      // CREATE events
      const externalId = "duplicate_external_id";
      const event1 = await dbHelper.createTestEvent({ calendar, externalId });
      const event2 = await dbHelper.createTestEvent({ calendar, externalId });
  
      // DELETE event
      await eventHelper.deleteEvent(event1.externalId, 409);
    });

    it("Event - Create chat message - 404 meeting not found", async function (): Promise<void> {
      // CREATE facility
      const facility = await dbHelper.createTestFacility();

      // CREATE chat message
      const meetingId = "UNKNOWN_MEETING_ID";
      const content = `Message content for team ID ${facility.team.msTeamId} and meeting ID ${meetingId}`;
      await eventHelper.createEventMessage(facility.team.msTeamId, meetingId, content, 404);
    });

    it("Event - Create chat message - 404 event Inactive", async function (): Promise<void> {
      // CREATE facility
      const facility = await dbHelper.createTestFacility();

      // CREATE calendar
      const calendar = await dbHelper.createTestCalendar({ facility });


      // CREATE event
      const event = await dbHelper.createTestEvent({ calendar, createMeeting: true });
    
      // DELETE event
      await eventHelper.deleteEvent(event.externalId);

      // CREATE chat message
      const content = `Message content for team ID ${facility.team.msTeamId} and meeting ID ${event.msMeetingId!}`;
      await eventHelper.createEventMessage(facility.team.msTeamId, event.msMeetingId!, content, 404);
    });

    it("Event - Create chat message - 400 chat message cannot be blank", async function (): Promise<void> {
      const teamId = LookupHelper.createTestGuid();
      const meetingId = LookupHelper.createTestGuid();
      await eventHelper.createEventMessage(teamId, meetingId, "", 400);
    });
  });
});
