import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { SubregionFixture } from "./subregion-fixture";
import { expect } from "chai";
import { AxiosResponse } from "axios";
import { ISubRegion } from "../../helpers/cosmos-helper";

const schemaProvider = new SchemaProvider();

export class SubRegionHelper {

  public async getSubRegions(): Promise<AxiosResponse<ISubRegion[]>> {
    const focusConnectApi = new SubregionFixture();
    const getResponse = await focusConnectApi.getSubRegions();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.SubRegionLocationList), getResponse.data);
    return getResponse;
  }
  public async getSubRegionsByRegion(countryId: string, stateId: string, regionId: string): Promise<AxiosResponse<ISubRegion[]>> {
    const focusConnectApi = new SubregionFixture();
    const getResponse = await focusConnectApi.getSubRegionsByRegion(countryId, stateId, regionId);
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.SubRegionLocationList), getResponse.data);
    return getResponse;
  }

  public async getSubRegion(countryId: string, stateId: string, regionId: string, subRegionId: string): Promise<AxiosResponse<ISubRegion | undefined>> {
    const focusConnectApi = new SubregionFixture();
    const getResponse = await focusConnectApi.getSubRegion(countryId, stateId, regionId, subRegionId);
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.SubRegionLocation), getResponse.data);
    return getResponse;
  }

  public async createSubRegion(countryId: string, stateId: string, regionId: string, subregion: ISubRegion, expectedStatus: number = 201): Promise<AxiosResponse<ISubRegion>> {
    const focusConnectApi = new SubregionFixture();
    console.log("Creating subregion " + subregion.id);
    let createResponse = await focusConnectApi.createSubRegion(countryId, stateId, regionId, subregion)
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.SubRegionLocation), createResponse.data);
    return createResponse;
  }

  public async updateSubRegion(countryId: string, stateId: string, regionId: string, subRegionId: string, subregion: ISubRegion, expectedStatus: number = 200): Promise<AxiosResponse<ISubRegion>> {
    const focusConnectApi = new SubregionFixture();
    console.log("Updating subregion " + stateId);
    let updateResponse = await focusConnectApi.updateSubRegion(countryId, stateId, regionId, subRegionId, subregion);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.SubRegionLocation), updateResponse.data);
    return updateResponse;
  }

  public async deleteSubRegion(countryId: string, stateId: string, regionId: string, subRegionId: string, expectedStatus: number = 204): Promise<AxiosResponse<any>> {
    const focusConnectApi = new SubregionFixture();
    console.log("Deleting subregion " + stateId);
    const delResponse = await focusConnectApi.deleteSubRegion(countryId, stateId, regionId, subRegionId);
    expect(delResponse.status).to.equal(expectedStatus);
    return delResponse;
  }
}