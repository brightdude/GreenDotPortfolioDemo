import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { ISettingAccessLevel, ISettingChannel } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class TenantSettingsFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();
  
  constructor() {
    this.settings = new Settings();
  }

  async getAccessLevels(): Promise<AxiosResponse<ISettingAccessLevel[]>> {
    const path = `/settings/accessLevels`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getChannels(): Promise<AxiosResponse<ISettingChannel[]>> {
    const path = `/settings/channels`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getRecordingTypes(): Promise<AxiosResponse<any[]>> {
    const path = `/settings/recordingTypes`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getStreamTypes(): Promise<AxiosResponse<any[]>> {
    const path = `/settings/streamTypes`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async retrieveProvisioningStatusList(): Promise<AxiosResponse<any[]>> {
    const path = `/settings/provisioningstatusvalues/`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  };
}
