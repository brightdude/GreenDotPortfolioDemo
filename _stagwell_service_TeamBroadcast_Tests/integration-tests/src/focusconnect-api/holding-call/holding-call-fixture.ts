import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { ICalendarHoldingCall, ICalendarHoldingCalls, IChatMessage, IChatMessageCreateParams, IHoldingCall, IHoldingCallCreateParams, IMeetingConversationMembersResponse } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class HoldingCallFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();
  
  constructor() {
    this.settings = new Settings();
  }

  async getHoldingCalls(includeExpired?: boolean): Promise<AxiosResponse<ICalendarHoldingCall[]>> {
    let qs = "";
    if (includeExpired != null) {
      qs = `?includeExpired=${includeExpired ? "true" : "false"}`;
    }
    const path = `/holdingCall/${qs}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getHoldingCallsForCalendar(externalCalendarId: string, includeExpired?: boolean): Promise<AxiosResponse<ICalendarHoldingCalls[]>> {
    let qs = "";
    if (includeExpired != null) {
      qs = `?includeExpired=${includeExpired ? "true" : "false"}`;
    }
    const path = `/holdingCall/${externalCalendarId}${qs}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getHoldingCallsForTeam(teamId: string, includeExpired?: boolean): Promise<AxiosResponse<ICalendarHoldingCalls[]>> {
    let qs = "";
    if (includeExpired != null) {
      qs = `?includeExpired=${includeExpired ? "true" : "false"}`;
    }
    const path = `/teams/${teamId}/holdingCalls${qs}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createHoldingCall(externalCalendarId: string, holdingCall: IHoldingCallCreateParams): Promise<AxiosResponse<IHoldingCall>> {
    const path = `/holdingCall/${externalCalendarId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, holdingCall, this.settings.RequestConfig);
  }

  async deleteHoldingCall(externalCalendarId: string): Promise<AxiosResponse<any>> {
    const path = `/holdingCall/${externalCalendarId}`
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path);
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }

  async createHoldingCallMessage(msTeamId: string, msMeetingId: string, chatMessageRequest: IChatMessageCreateParams): Promise<AxiosResponse<IChatMessage>> {
    const path = `/teams/${msTeamId}/holdingCall/${msMeetingId}/messages`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, chatMessageRequest, this.settings.RequestConfig);
  }

  async getMembersForTeam(teamId: string): Promise<AxiosResponse<IMeetingConversationMembersResponse[]>> {
    const path = `/teams/${teamId}/holdingCalls/members`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }
}