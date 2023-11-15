import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { EventFixture } from "./event-fixture";
import { expect } from "chai";
import { IChatMessage, IEvent, IEventConversationMembersResponse, IEventCreateUpdateParams } from "../../helpers/cosmos-helper";
import { AxiosResponse } from "axios";
import moment from "moment";

const schemaProvider = new SchemaProvider();

export class EventHelper {
  private apiFixture: EventFixture;

  constructor() {
    this.apiFixture = new EventFixture();
  };

  public async getEvents(): Promise<AxiosResponse<IEvent[]>> {
    const getResponse = await this.apiFixture.getEvents();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.EventList), getResponse.data);
    return getResponse;
  }

  public async getEventsForTeam(teamId: string): Promise<AxiosResponse<IEvent[]>> {
    const getResponse = await this.apiFixture.getEventsForTeam(teamId);
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.EventList), getResponse.data);
    return getResponse;
  }

  public async getEvent(eventExternalId: string, expectedStatus: number = 200): Promise<AxiosResponse<IEvent | undefined>> {
    const getResponse = await this.apiFixture.getEvent(eventExternalId);
    expect(getResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.Event), getResponse.data);
    return getResponse;
  }

  public async createEvent(externalId: string, event: IEventCreateUpdateParams, expectedStatus: number = 201): Promise<AxiosResponse<IEvent>> {
    console.log("Creating event " + externalId);
    const createResponse = await this.apiFixture.createEvent(externalId, event);
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.Event), createResponse.data);
    return createResponse;
  }

  public async updateEvent(externalId: string, event: IEventCreateUpdateParams, expectedStatus: number = 200): Promise<AxiosResponse<IEvent>> {
    console.log("Updating event " + externalId);
    const updateResponse = await this.apiFixture.updateEvent(externalId, event);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.Event), updateResponse.data);
    return updateResponse;
  }

  public async deleteEvent(externalId: string, expectedStatus: number = 204): Promise<any> {
    console.log("Deleting event " + externalId);
    const delResponse = await this.apiFixture.deleteEvent(externalId);
    expect(delResponse.status).to.equal(expectedStatus);
    return delResponse;
  }

  public async createEventMessage(msTeamId: string, msMeetingId: string, content: string, expectedStatus: number = 201): Promise<AxiosResponse<IChatMessage>> {
    console.log(`Creating calendar event message for meeting id '${msMeetingId}'...`);
    const createResponse = await this.apiFixture.createEventMessage(msTeamId, msMeetingId, { meetingId: msMeetingId, teamId: msTeamId, content });
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.ChatMessage), createResponse.data);
    return createResponse;
  }
  
  public createTestEventRequestBody(externalId: string, externalCalendarId: string): IEventCreateUpdateParams {
    const now = moment();
    let month = now;
    month = month.add(1, "month");
    const startTime = moment(month).toDate();
    const endTime = month.add(1, "hour").toDate();

    const caseId = `CASE${externalId}`;
    const caseTitle = "Spy vs Spy";

    return {
      body: "email body",
      caseId,
      caseTitle,
      caseType: "federal",
      endTime: endTime.toISOString(),
      eventType: "virtual",
      externalCalendarId,
      optionalAttendees: ["jtingey-labs@fortherecord.com"],
      requiredAttendees: ["jtingey@fortherecord.com"],
      startTime: startTime.toISOString(),
      subject: `__test_CASE${externalId} - Spy vs Spy`
    };
  }

  public async getMembersForTeam(teamId: string): Promise<AxiosResponse<IEventConversationMembersResponse[]>> {
    const getResponse = await this.apiFixture.getMembersForTeam(teamId);
    expect(getResponse.status).to.equal(200);
    return getResponse;
  }
}