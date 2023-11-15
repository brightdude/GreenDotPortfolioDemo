import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { IRegion } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class RegionFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getRegions(): Promise<AxiosResponse<IRegion[]>> {
    const path = `/regions`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getRegionsByCountry(countryId: string, stateId: string): Promise<AxiosResponse<IRegion[]>> {
    const path = `/countries/${countryId}/states/${stateId}/regions`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getRegion(countryId: string, stateId: string, regionId: string): Promise<AxiosResponse<IRegion | undefined>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createRegion(countryId: string, stateId: string, region: IRegion): Promise<AxiosResponse<IRegion>> {
    const path = `/countries/${countryId}/states/${stateId}/regions`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, region, this.settings.RequestConfig);
  }

  async updateRegion(countryId: string, stateId: string, regionId: string, region: IRegion): Promise<AxiosResponse<IRegion>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, region, this.settings.RequestConfig);
  }

  async deleteRegion(countryId: string, stateId: string, regionId: string): Promise<AxiosResponse<any>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }
}