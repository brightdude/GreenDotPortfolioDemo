import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { IChatMessage, IChatMessageCreateParams, IEvent, IEventConversationMembersResponse, IEventCreateUpdateParams } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class EventFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getEvents(): Promise<AxiosResponse<IEvent[]>> {
    const path = `/event`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getEventsForTeam(teamId: string): Promise<AxiosResponse<IEvent[]>> {
    const path = `/teams/${teamId}/events`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getEvent(eventExternalId: string): Promise<AxiosResponse<IEvent | undefined>> {
    const path = `/event/${eventExternalId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createEvent(eventExternalId: string, event: IEventCreateUpdateParams): Promise<AxiosResponse<IEvent>> {
    const path = `/event/${eventExternalId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, event, this.settings.RequestConfig);
  }

  async updateEvent(eventExternalId: string, event: IEventCreateUpdateParams): Promise<AxiosResponse<IEvent>> {
    const path = `/event/${eventExternalId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, event, this.settings.RequestConfig);
  }

  async deleteEvent(eventExternalId: string): Promise<AxiosResponse<any>> {
    const path = `/event/${eventExternalId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }

  async createEventMessage(msTeamId: string, msMeetingId: string, chatMessageRequest: IChatMessageCreateParams): Promise<AxiosResponse<IChatMessage>> {
    const path = `/teams/${msTeamId}/events/${msMeetingId}/messages`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, chatMessageRequest, this.settings.RequestConfig);
  }

  async getMembersForTeam(teamId: string): Promise<AxiosResponse<IEventConversationMembersResponse[]>> {
    const path = `/teams/${teamId}/events/members`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }
}
