import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { ILookupItem } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class PersonnelRoleFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getPersonnelRoles(): Promise<AxiosResponse<ILookupItem[]>> {
    const path = `/personnelRoles`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getPersonnelRole(personnelRoleId: string): Promise<AxiosResponse<ILookupItem | undefined>> {
    const path = `/personnelRoles/${personnelRoleId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createPersonnelRole(personnelRole: ILookupItem): Promise<AxiosResponse<ILookupItem>> {
    const path = `/personnelRoles`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, personnelRole, this.settings.RequestConfig);
  }

  async updatePersonnelRole(personnelRoleId: string, personnelRole: ILookupItem): Promise<AxiosResponse<ILookupItem>> {
    const path = `/personnelRoles/${personnelRoleId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, personnelRole, this.settings.RequestConfig);
  }

  async deletePersonnelRole(personnelRoleId: string): Promise<AxiosResponse<any>> {
    const path = `/personnelRoles/${personnelRoleId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }
}