import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { OnDemandMeetingFixture } from "./on-demand-meeting-fixture";
import { expect } from "chai";
import { IOnDemandMeeting, IOnDemandMeetingCreateParams, IChatMessageCreateParams, IEventConversationMembersResponse } from "../../helpers/cosmos-helper";
import { AxiosResponse } from "axios";

const schemaProvider = new SchemaProvider();

export class OnDemandMeetingHelper {
  public async retrieveOnDemandMeetingList(teamId: string, expectedStatus: number = 200): Promise<AxiosResponse<IOnDemandMeeting[]>> {
    const focusApi = new OnDemandMeetingFixture();
    const retrieveResponse = await focusApi.retrieveOnDemandMeetings(teamId);
    expect(retrieveResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.OnDemandMeetingList), retrieveResponse.data);
    return retrieveResponse;
  }

  public async createOnDemandMeeting(teamId: string, onDemandMeetingRequest: IOnDemandMeetingCreateParams, expectedStatus: number = 201): Promise<AxiosResponse<IOnDemandMeeting>> {
    const focusApi = new OnDemandMeetingFixture();
    console.log(`Creating on-demand meeting '${onDemandMeetingRequest.meetingName}'`);
    const createResponse = await focusApi.createOnDemandMeeting(teamId, onDemandMeetingRequest);
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.OnDemandMeeting), createResponse.data);
    return createResponse;
  }

  public async deleteOnDemandMeeting(teamId: string, meetingId: string, expectedStatus: number = 204): Promise<AxiosResponse<IOnDemandMeeting>> {
    const focusApi = new OnDemandMeetingFixture();
    console.log(`Deleting on-demand meeting '${meetingId}'`);
    const deleteResponse = await focusApi.deleteOnDemandMeeting(teamId, meetingId);
    expect(deleteResponse.status).to.equal(expectedStatus);
    return deleteResponse;
  }

  public async createOnDemandMeetingMessage(teamId: string, meetingId: string, chatMessageRequest: IChatMessageCreateParams, expectedStatus: number = 201): Promise<AxiosResponse<any>> {
    const focusApi = new OnDemandMeetingFixture();
    console.log(`Creating on-demand meeting message for meeting id '${chatMessageRequest.meetingId}'`);
    const createResponse = await focusApi.createOnDemandMeetingMessage(teamId, meetingId, chatMessageRequest);
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.ChatMessage), createResponse.data);
    return createResponse;
  }

  public async getMembersForTeam(teamId: string): Promise<AxiosResponse<IEventConversationMembersResponse[]>> {
    const focusApi = new OnDemandMeetingFixture();
    const getResponse = await focusApi.getMembersForTeam(teamId);
    expect(getResponse.status).to.equal(200);
    return getResponse;
  }
}