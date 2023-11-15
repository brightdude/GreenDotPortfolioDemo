import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { IFacility, IFacilityCreateUpdateParams } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class FacilityFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getFacilities(): Promise<AxiosResponse<IFacility[]>> {
    const path = `/facilities`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getFacility(facilityId: string): Promise<AxiosResponse<IFacility | undefined>> {
    const path = `/facilities/${facilityId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createFacility(facility: IFacilityCreateUpdateParams): Promise<AxiosResponse<IFacility>> {
    const path = `/facilities`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, facility, this.settings.RequestConfig);
  }

  async updateFacility(facilityId: string, facility: IFacilityCreateUpdateParams): Promise<AxiosResponse<IFacility>> {
    const path = `/facilities/${facilityId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, facility, this.settings.RequestConfig);
  }

  async deleteFacility(facilityId: string): Promise<AxiosResponse<any>> {
    const path = `/facilities/${facilityId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }

  async getTeamIdByChannelId(msChannelId: string): Promise<AxiosResponse<string>> {
    const path = `/channels/${msChannelId}/team`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }
}
