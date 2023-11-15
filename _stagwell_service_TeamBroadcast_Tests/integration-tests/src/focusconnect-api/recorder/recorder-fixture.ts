import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { Settings } from "../../helpers/settings-helper";

export interface ICreateRecorderRequest {
  departmentId: string,
  displayName: string,
  locationName: string,
  recordingTypeId: string,
  streamTypeId: string
}

export interface IUpdateRecorderRequest {
  departmentId: string,
  displayName?: string,
  locationName?: string,
  provisioningStatus?: string,
  recordingTypeId: string,
  streamTypeId: string
}

export class RecorderFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();
  
  constructor() {
    this.settings = new Settings();
  }

  async retrieveRecorders(): Promise<AxiosResponse<any>> {
    const path = `/recorders`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  };

  async getRecorder(id: string): Promise<AxiosResponse<any>> {
    const path = `/recorders/${id}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  };

  async createRecorder(request: ICreateRecorderRequest): Promise<AxiosResponse<any>> {
    const path = `/recorders`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, request);
  };

  async updateRecorder(id: string, request: IUpdateRecorderRequest): Promise<AxiosResponse<any>> {
    const path = `/recorders/${id}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, request);
  };

  async deleteRecorder(id: string): Promise<AxiosResponse<any>> {
    const path = `/recorders/${id}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri);
  };
}
