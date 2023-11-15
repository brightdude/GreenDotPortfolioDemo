import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { ILookupItem } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class TitleFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getTitles(): Promise<AxiosResponse<ILookupItem[]>> {
    const path = `/titles`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getTitle(titleId: string): Promise<AxiosResponse<ILookupItem | undefined>> {
    const path = `/titles/${titleId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createTitle(title: ILookupItem): Promise<AxiosResponse<ILookupItem>> {
    const path = `/titles`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, title, this.settings.RequestConfig);
  }

  async updateTitle(titleId: string, title: ILookupItem): Promise<AxiosResponse<ILookupItem>> {
    const path = `/titles/${titleId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, title, this.settings.RequestConfig);
  }

  async deleteTitle(titleId: string): Promise<AxiosResponse<any>> {
    const path = `/titles/${titleId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }
}