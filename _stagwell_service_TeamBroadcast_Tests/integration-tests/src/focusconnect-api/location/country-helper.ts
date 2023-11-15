import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { CountryFixture } from "./country-fixture";
import { expect } from "chai";
import { AxiosResponse } from "axios";
import { ILocation } from "../../helpers/cosmos-helper";

const schemaProvider = new SchemaProvider();

export class CountryHelper {
  public async getCountries(): Promise<AxiosResponse<ILocation[]>> {
    const focusConnectApi = new CountryFixture();
    const getResponse = await focusConnectApi.getCountries();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.CountryLocationList), getResponse.data);
    return getResponse;
  }

  public async getCountry(countryId: string): Promise<AxiosResponse<ILocation | undefined>> {
    const focusConnectApi = new CountryFixture();
    const getResponse = await focusConnectApi.getCountry(countryId);
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.CountryLocation), getResponse.data);
    return getResponse;
  }

  public async createCountry(country: ILocation, expectedStatus: number = 201): Promise<AxiosResponse<ILocation>> {
    const focusConnectApi = new CountryFixture();
    console.log("Creating country " + country.id);
    let createResponse = await focusConnectApi.createCountry(country)
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.CountryLocation), createResponse.data);
    return createResponse;
  }

  public async updateCountry(countryId: string, country: ILocation, expectedStatus: number = 200): Promise<AxiosResponse<ILocation>> {
    const focusConnectApi = new CountryFixture();
    console.log("Updating country " + countryId);
    let updateResponse = await focusConnectApi.updateCountry(countryId, country);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.CountryLocation), updateResponse.data);
    return updateResponse;
  }

  public async deleteCountry(countryId: string, expectedStatus: number = 204): Promise<any> {
    const focusConnectApi = new CountryFixture();
    console.log("Deleting country " + countryId);
    const delResponse = await focusConnectApi.deleteCountry(countryId);
    expect(delResponse.status).to.equal(expectedStatus);
  }
}