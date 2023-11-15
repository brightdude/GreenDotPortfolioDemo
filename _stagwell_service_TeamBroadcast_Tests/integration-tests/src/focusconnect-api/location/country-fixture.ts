import { AxiosResponse } from "axios";
import { ApimOauthHelper } from "../../helpers/apim-oauth-helper";
import { ILocation } from "../../helpers/cosmos-helper";
import { Settings } from "../../helpers/settings-helper";

export class CountryFixture {
  apiUriSuffix = "/focus-connect";
  settings: Settings;

  private apimOauthHelper = new ApimOauthHelper();

  constructor() {
    this.settings = new Settings();
  }

  async getCountries(): Promise<AxiosResponse<ILocation[]>> {
    const path = `/countries`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async getCountry(countryId: string): Promise<AxiosResponse<ILocation>> {
    const path = `/countries/${countryId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.get(requestUri, this.settings.RequestConfig);
  }

  async createCountry(country: ILocation): Promise<AxiosResponse<ILocation>> {
    const path = `/countries`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.post(requestUri, country, this.settings.RequestConfig);
  }

  async updateCountry(countryId: string, country: ILocation): Promise<AxiosResponse<ILocation>> {
    const path = `/countries/${countryId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.patch(requestUri, country, this.settings.RequestConfig);
  }

  async deleteCountry(countryId: string): Promise<AxiosResponse<any>> {
    const path = `/countries/${countryId}`;
    const requestUri = this.settings.BuildRequestUri(this.apiUriSuffix, path)
    return await this.apimOauthHelper.delete(requestUri, this.settings.RequestConfig);
  }
}