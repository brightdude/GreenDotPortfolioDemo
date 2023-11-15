import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { BuildingFixture } from "./building-fixture";
import { expect } from "chai";
import { AxiosResponse } from "axios";
import { IBuilding } from "../../helpers/cosmos-helper";

const schemaProvider = new SchemaProvider();

export class BuildingHelper {
  public async getBuildings(): Promise<AxiosResponse<IBuilding[]>> {
    const focusConnectApi = new BuildingFixture();
    const getResponse = await focusConnectApi.getBuildings();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.BuildingLocationList), getResponse.data);
    return getResponse;
  }

  public async getBuildingsBySubRegion(countryId: string, stateId: string, regionId: string, subRegionId: string): Promise<AxiosResponse<IBuilding[]>> {
    const focusConnectApi = new BuildingFixture();
    const getResponse = await focusConnectApi.getBuildingsBySubRegion(countryId, stateId, regionId, subRegionId);
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.BuildingLocationList), getResponse.data);
    return getResponse;
  }

  public async getBuilding(countryId: string, stateId: string, regionId: string, subRegionId: string, buildingId: string): Promise<AxiosResponse<IBuilding | undefined>> {
    const focusConnectApi = new BuildingFixture();
    const getResponse = await focusConnectApi.getBuilding(countryId, stateId, regionId, subRegionId, buildingId);
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.BuildingLocation), getResponse.data);
    return getResponse;
  }

  public async createBuilding(countryId: string, stateId: string, regionId: string, subRegionId: string, building: IBuilding, expectedStatus: number = 201): Promise<AxiosResponse<IBuilding>> {
    const focusConnectApi = new BuildingFixture();
    console.log("Creating building " + building.id);
    let createResponse = await focusConnectApi.createBuilding(countryId, stateId, regionId, subRegionId, building)
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.BuildingLocation), createResponse.data);
    return createResponse;
  }

  public async updateBuilding(countryId: string, stateId: string, regionId: string, subRegionId: string, buildingId: string, building: IBuilding, expectedStatus: number = 200): Promise<AxiosResponse<IBuilding>> {
    const focusConnectApi = new BuildingFixture();
    console.log("Updating building " + stateId);
    let updateResponse = await focusConnectApi.updateBuilding(countryId, stateId, regionId, subRegionId, buildingId, building);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.BuildingLocation), updateResponse.data);
    return updateResponse;
  }

  public async deleteBuilding(countryId: string, stateId: string, regionId: string, subRegionId: string, buildingId: string, expectedStatus: number = 204): Promise<AxiosResponse<any>> {
    const focusConnectApi = new BuildingFixture();
    console.log("Deleting building " + stateId);
    const delResponse = await focusConnectApi.deleteBuilding(countryId, stateId, regionId, subRegionId, buildingId);
    expect(delResponse.status).to.equal(expectedStatus);
    return delResponse;
  }
}