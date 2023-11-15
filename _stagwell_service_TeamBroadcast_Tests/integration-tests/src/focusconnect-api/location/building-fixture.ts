import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { IBuilding } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class BuildingFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getBuildings(): Promise<AxiosResponse<IBuilding[]>> {
    const path = `/buildings`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }
  async getBuildingsBySubRegion(countryId: string, stateId: string, regionId: string, subRegionId: string): Promise<AxiosResponse<IBuilding[]>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}/subregions/${subRegionId}/buildings`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getBuilding(countryId: string, stateId: string, regionId: string, subRegionId: string, buildingId: string): Promise<AxiosResponse<IBuilding | undefined>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}/subregions/${subRegionId}/buildings/${buildingId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createBuilding(countryId: string, stateId: string, regionId: string, subRegionId: string, building: IBuilding): Promise<AxiosResponse<IBuilding>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}/subregions/${subRegionId}/buildings`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, building, this.settings.RequestConfig);
  }

  async updateBuilding(countryId: string, stateId: string, regionId: string, subRegionId: string, buildingId: string, building: IBuilding): Promise<AxiosResponse<IBuilding>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}/subregions/${subRegionId}/buildings/${buildingId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, building, this.settings.RequestConfig);
  }

  async deleteBuilding(countryId: string, stateId: string, regionId: string, subRegionId: string, buildingId: string): Promise<AxiosResponse<any>> {
    const path = `/countries/${countryId}/states/${stateId}/regions/${regionId}/subregions/${subRegionId}/buildings/${buildingId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }
}