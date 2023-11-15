import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { HoldingCallFixture } from "./holding-call-fixture";
import { expect } from "chai";
import { AxiosResponse } from "axios";
import { CosmosDbHelper, ICalendar, ICalendarHoldingCall, ICalendarHoldingCalls, IChatMessage, IChatMessageCreateParams, IHoldingCall, IHoldingCallCreateParams, IMeetingConversationMembersResponse } from "../../helpers/cosmos-helper";
import moment from "moment";

const schemaProvider = new SchemaProvider();
const holdingCallFixture = new HoldingCallFixture();
const dbHelper = new CosmosDbHelper();

export class HoldingCallHelper {
    public async getHoldingCalls(includeExpired: boolean, expectedStatus: number = 200): Promise<AxiosResponse<ICalendarHoldingCall[]>> {
        const getResponse = await holdingCallFixture.getHoldingCalls(includeExpired);
        expect(getResponse.status).to.equal(expectedStatus);
        if (expectedStatus == 200)
            await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.CalendarHoldingCallList), getResponse.data);
        return getResponse;
    }

    public async getHoldingCallsForCalendar(externalCalendarId: string, includeExpired: boolean, expectedStatus: number = 200): Promise<AxiosResponse<ICalendarHoldingCalls[]>> {
        const getResponse = await holdingCallFixture.getHoldingCallsForCalendar(externalCalendarId, includeExpired);
        expect(getResponse.status).to.equal(expectedStatus);
        if (expectedStatus == 200)
            await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.CalendarHoldingCallsList), getResponse.data);
        return getResponse;
    }

    public async getHoldingCallsForTeam(teamId: string, includeExpired: boolean, expectedStatus: number = 200): Promise<AxiosResponse<ICalendarHoldingCalls[]>> {
        let getResponse: AxiosResponse<any>;
        try {
            getResponse = await holdingCallFixture.getHoldingCallsForTeam(teamId, includeExpired);
        }
        catch (ex) {
            getResponse = (<any>ex).response;
        }
        expect(getResponse.status).to.equal(expectedStatus);
        if (expectedStatus == 200)
            await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.CalendarHoldingCallsList), getResponse.data);
        return getResponse;
    }

    public async createHoldingCall(externalCalendarId: string, input: IHoldingCallCreateParams, expectedStatus: number = 201): Promise<AxiosResponse<IHoldingCall>> {
        console.log("Creating holding call for calendar " + externalCalendarId);
        const createResponse = await holdingCallFixture.createHoldingCall(externalCalendarId, input);
        expect(createResponse.status).to.equal(expectedStatus);

        if (expectedStatus == 201)
            await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.HoldingCall), createResponse.data);
        return createResponse;
    }

    public async deleteHoldingCall(externalCalendarId: string, expectedStatus: number = 204): Promise<AxiosResponse<any>> {
        console.log("Deleting holding call for calendar " + externalCalendarId);
        const delResponse = await holdingCallFixture.deleteHoldingCall(externalCalendarId);
        expect(delResponse.status).to.equal(expectedStatus);
        return delResponse;
    }

    public async createHoldingCallMessage(msTeamId: string, msMeetingId: string, chatMessageRequest: IChatMessageCreateParams, expectedStatus: number = 201): Promise<AxiosResponse<IChatMessage>> {
        console.log(`Creating holding call message for meeting id '${chatMessageRequest.meetingId}'`);
        const createResponse = await holdingCallFixture.createHoldingCallMessage(msTeamId, msMeetingId, chatMessageRequest);
        expect(createResponse.status).to.equal(expectedStatus);

        if (expectedStatus == 201)
            await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.ChatMessage), createResponse.data);
        return createResponse;
    }

    public async createHoldingCallCreateRequestBody(options?: { createInPast?: boolean, calendar?: ICalendar }): Promise<IHoldingCallCreateParams> {
        const calendar = options?.calendar ?? (await dbHelper.createTestCalendar());

        const now = moment();
        let month = now;
        if (options?.createInPast)
            month = month.subtract(1, "month");
        else
            month = month.add(1, "month");
        const startTime = moment(month).format();
        const endTime = month.add(1, "hour").format();

        // Calendar must exist
        const data: IHoldingCallCreateParams = {
            externalCalendarId: calendar.externalCalendarId,
            startTime: startTime,
            endTime: endTime
        };

        return data;
    };

    public async getMembersForTeam(teamId: string): Promise<AxiosResponse<IMeetingConversationMembersResponse[]>> {
        const getResponse = await holdingCallFixture.getMembersForTeam(teamId);
        expect(getResponse.status).to.equal(200);
        return getResponse;
    }
}