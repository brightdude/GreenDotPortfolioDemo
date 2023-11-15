import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { IState } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class StateFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getStates(): Promise<AxiosResponse<IState[]>> {
    const path = `/states`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getStatesByCountry(countryId: string): Promise<AxiosResponse<IState[]>> {
    const path = `/countries/${countryId}/states`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getState(countryId: string, stateId: string): Promise<AxiosResponse<IState | undefined>> {
    const path = `/countries/${countryId}/states/${stateId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createState(countryId: string, state: IState): Promise<AxiosResponse<IState>> {
    const path = `/countries/${countryId}/states`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, state, this.settings.RequestConfig);
  }

  async updateState(countryId: string, stateId: string, state: IState): Promise<AxiosResponse<IState>> {
    const path = `/countries/${countryId}/states/${stateId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, state, this.settings.RequestConfig);
  }

  async deleteState(countryId: string, stateId: string): Promise<AxiosResponse<any>> {
    const path = `/countries/${countryId}/states/${stateId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }
}