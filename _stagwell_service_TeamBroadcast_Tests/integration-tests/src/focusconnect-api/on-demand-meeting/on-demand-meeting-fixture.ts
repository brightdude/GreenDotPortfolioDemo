import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { IOnDemandMeeting, IOnDemandMeetingCreateParams, IChatMessageCreateParams, IEventConversationMembersResponse } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class OnDemandMeetingFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();
  
  constructor() {
    this.settings = new Settings();
  }

  async retrieveOnDemandMeetings(teamId: string): Promise<AxiosResponse<IOnDemandMeeting[]>> {
    const path = `/teams/${teamId}/onDemandMeetings`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  };

  async createOnDemandMeeting(teamId: string, onDemandMeetingRequest: IOnDemandMeetingCreateParams): Promise<AxiosResponse<IOnDemandMeeting>> {
    const path = `/teams/${teamId}/onDemandMeetings`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, onDemandMeetingRequest, this.settings.RequestConfig);
  }

  async deleteOnDemandMeeting(teamId: string, meetingId: string): Promise<AxiosResponse<IOnDemandMeeting>> {
    const path = `/teams/${teamId}/onDemandMeetings/${meetingId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }

  async createOnDemandMeetingMessage(teamId: string, meetingId: string, chatMessageRequest: IChatMessageCreateParams): Promise<AxiosResponse<IOnDemandMeeting>> {
    const path = `/teams/${teamId}/onDemandMeetings/${meetingId}/messages`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, chatMessageRequest, this.settings.RequestConfig);
  }

  async getMembersForTeam(teamId: string): Promise<AxiosResponse<IEventConversationMembersResponse[]>> {
    const path = `/teams/${teamId}/onDemandMeetings/members`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }
}
