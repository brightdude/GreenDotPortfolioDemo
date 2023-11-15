import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { RegionFixture } from "./region-fixture";
import { expect } from "chai";
import { AxiosResponse } from "axios";
import { IRegion } from "../../helpers/cosmos-helper";

const schemaProvider = new SchemaProvider();

export class RegionHelper {

  public async getRegions(): Promise<AxiosResponse<IRegion[]>> {
    const focusConnectApi = new RegionFixture();
    const getResponse = await focusConnectApi.getRegions();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.RegionLocationList), getResponse.data);
    return getResponse;
  }

  public async getRegionsByState(countryId: string, stateId: string): Promise<AxiosResponse<IRegion[]>> {
    const focusConnectApi = new RegionFixture();
    const getResponse = await focusConnectApi.getRegionsByCountry(countryId, stateId);
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.RegionLocationList), getResponse.data);
    return getResponse;
  }

  public async getRegion(countryId: string, stateId: string, regionId: string): Promise<AxiosResponse<IRegion | undefined>> {
    const focusConnectApi = new RegionFixture();
    const getResponse = await focusConnectApi.getRegion(countryId, stateId, regionId);
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.RegionLocation), getResponse.data);
    return getResponse;
  }

  public async createRegion(countryId: string, stateId: string, region: IRegion, expectedStatus: number = 201): Promise<AxiosResponse<IRegion>> {
    const focusConnectApi = new RegionFixture();
    console.log("Creating region " + region.id);
    let createResponse = await focusConnectApi.createRegion(countryId, stateId, region)
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.RegionLocation), createResponse.data);
    return createResponse;
  }

  public async updateRegion(countryId: string, stateId: string, regionId: string, region: IRegion, expectedStatus: number = 200): Promise<AxiosResponse<IRegion>> {
    const focusConnectApi = new RegionFixture();
    console.log("Updating region " + stateId);
    let updateResponse = await focusConnectApi.updateRegion(countryId, stateId, regionId, region);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.RegionLocation), updateResponse.data);
    return updateResponse;
  }

  public async deleteRegion(countryId: string, stateId: string, regionId: string, expectedStatus: number = 204): Promise<AxiosResponse<any>> {
    const focusConnectApi = new RegionFixture();
    console.log("Deleting region " + stateId);
    const delResponse = await focusConnectApi.deleteRegion(countryId, stateId, regionId);
    expect(delResponse.status).to.equal(expectedStatus);
    return delResponse;
  }
}