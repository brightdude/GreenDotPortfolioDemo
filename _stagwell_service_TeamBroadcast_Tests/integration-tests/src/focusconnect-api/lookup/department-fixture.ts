import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { ILookupItem } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class DepartmentFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getDepartments(): Promise<AxiosResponse<ILookupItem[]>> {
    const path = `/departments`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getDepartment(departmentId: string): Promise<AxiosResponse<ILookupItem | undefined>> {
    const path = `/departments/${departmentId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createDepartment(department: ILookupItem): Promise<AxiosResponse<ILookupItem>> {
    const path = `/departments`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, department, this.settings.RequestConfig);
  }

  async updateDepartment(departmentId: string, department: ILookupItem): Promise<AxiosResponse<ILookupItem>> {
    const path = `/departments/${departmentId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, department, this.settings.RequestConfig);
  }

  async deleteDepartment(departmentId: string): Promise<AxiosResponse<any>> {
    const path = `/departments/${departmentId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }
}