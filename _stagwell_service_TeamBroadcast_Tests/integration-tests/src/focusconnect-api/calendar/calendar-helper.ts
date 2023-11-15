import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { CalendarFixture } from "./calendar-fixture";
import { expect } from "chai";
import { CosmosDbHelper, ICalendar, ICalendarCreateUpdateParams, ICalendarSummary, IFacility, ILookupItem, IRecorder, IUser } from "../../helpers/cosmos-helper";
import { AxiosResponse } from "axios";

const schemaProvider = new SchemaProvider();
const calendarFixture = new CalendarFixture();

export class CalendarHelper {
  public async getCalendars(): Promise<AxiosResponse<ICalendarSummary[]>> {
    const getResponse = await calendarFixture.getCalendars();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.CalendarList), getResponse.data);
    return getResponse;
  }

  public async getCalendar(externalCalendarId: string, expectedStatus: number = 200): Promise<AxiosResponse<ICalendar | undefined>> {
    const getResponse = await calendarFixture.getCalendar(externalCalendarId);
    expect(getResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.Calendar), getResponse.data);
    return getResponse;
  }

  public async createCalendar(calendar: ICalendarCreateUpdateParams, expectedStatus: number = 201): Promise<AxiosResponse<ICalendar>> {
    console.log(`Creating calendar '${calendar.externalCalendarId}...'`);
    const updateResponse = await calendarFixture.createCalendar(calendar);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.Calendar), updateResponse.data);
    return updateResponse;
  }

  public async updateCalendar(externalCalendarId: string, calendar: ICalendarCreateUpdateParams, expectedStatus: number = 200): Promise<AxiosResponse<ICalendar>> {
    console.log(`Updating calendar '${externalCalendarId}...'`);
    const updateResponse = await calendarFixture.updateCalendar(externalCalendarId, calendar);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.Calendar), updateResponse.data);
    return updateResponse;
  }

  public async deleteCalendar(externalCalendarId: string, expectedStatus: number = 204): Promise<AxiosResponse<any>> {
    console.log(`Deleting calendar '${externalCalendarId}...'`);
    const delResponse = await calendarFixture.deleteCalendar(externalCalendarId);
    expect(delResponse.status).to.equal(expectedStatus);
    return delResponse;
  }

  public async createCalendarRequestBody(options?: { facility?: IFacility, department?: ILookupItem, focusUsers?: IUser[], recorders?: IRecorder[] }): Promise<ICalendarCreateUpdateParams> {
    const dbHelper = new CosmosDbHelper();
    const facility = options?.facility ?? (await dbHelper.createTestFacility({ createTeam: true }));
    const department = options?.department ?? (await dbHelper.createTestLookup("Departments", "department"));
    const extId = (Math.random() * 9999).toFixed(0).padStart(4, "0");
    return {
      calendarName: `__test_Test Calendar ${extId}`,
      focusUsers: (options?.focusUsers ?? []).map(u => u.email),
      departmentId: department.id,
      externalCalendarId: `cal${extId}`,
      facilityId: facility.id,
      recorders: (options?.recorders ?? []).map(r => r.email),
      holdingCalls: []
    }
  }
}