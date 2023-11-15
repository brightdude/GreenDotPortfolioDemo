import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { ICalendar, ICalendarCreateUpdateParams, ICalendarSummary } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class CalendarFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getCalendars(): Promise<AxiosResponse<ICalendarSummary[]>> {
    const path = `/calendars`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getCalendar(externalCalendarId: string): Promise<AxiosResponse<ICalendar | undefined>> {
    const path = `/calendars/${externalCalendarId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createCalendar(calendar: ICalendarCreateUpdateParams): Promise<AxiosResponse<ICalendar>> {
    const path = `/calendars`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, calendar, this.settings.RequestConfig);
  }

  async updateCalendar(externalCalendarId: string, calendar: ICalendarCreateUpdateParams): Promise<AxiosResponse<ICalendar>> {
    const path = `/calendars/${externalCalendarId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, calendar, this.settings.RequestConfig);
  }

  async deleteCalendar(externalCalendarId: string): Promise<AxiosResponse<any>> {
    const path = `/calendars/${externalCalendarId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }
}
