import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { IUser, IUserCreateUpdateParams } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class UserFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getUsers(): Promise<AxiosResponse<IUser[]>> {
    const path = `/users`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getUser(emailAddress: string): Promise<AxiosResponse<IUser | undefined>> {
    const path = `/users/${emailAddress}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createUser(user: IUserCreateUpdateParams): Promise<AxiosResponse<IUser>> {
    const path = `/users`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, user, this.settings.RequestConfig);
  }

  async updateUser(emailAddress: string, user: IUserCreateUpdateParams): Promise<AxiosResponse<IUser>> {
    const path = `/users/${emailAddress}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, user, this.settings.RequestConfig);
  }

  async deleteUser(emailAddress: string): Promise<AxiosResponse<any>> {
    const path = `/users/${emailAddress}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }
}
