import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { ISubRegion } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class SubregionFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getSubRegions(): Promise<AxiosResponse<ISubRegion[]>> {
    const path = `/subregions`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getSubRegionsByRegion(countryId: string, stateId: string, regionId: string): Promise<AxiosResponse<ISubRegion[]>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}/subregions`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getSubRegion(countryId: string, stateId: string, regionId: string, subRegionId: string): Promise<AxiosResponse<ISubRegion | undefined>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}/subregions/${subRegionId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createSubRegion(countryId: string, stateId: string, regionId: string, subRegion: ISubRegion): Promise<AxiosResponse<ISubRegion>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}/subregions`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, subRegion, this.settings.RequestConfig);
  }

  async updateSubRegion(countryId: string, stateId: string, regionId: string, subRegionId: string, subRegion: ISubRegion): Promise<AxiosResponse<ISubRegion>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}/subregions/${subRegionId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, subRegion, this.settings.RequestConfig);
  }

  async deleteSubRegion(countryId: string, stateId: string, regionId: string, subRegionId: string): Promise<AxiosResponse<any>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}/subregions/${subRegionId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }
}