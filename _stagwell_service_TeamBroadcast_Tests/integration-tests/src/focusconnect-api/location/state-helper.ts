import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { StateFixture } from "./state-fixture";
import { expect } from "chai";
import { AxiosResponse } from "axios";
import { IState } from "../../helpers/cosmos-helper";

const schemaProvider = new SchemaProvider();

export class StateHelper {
  public async getStates(): Promise<AxiosResponse<IState[]>> {
    const focusConnectApi = new StateFixture();
    const getResponse = await focusConnectApi.getStates();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.StateLocationList), getResponse.data);
    return getResponse;
  }
  public async getStatesByCountry(countryId: string): Promise<AxiosResponse<IState[]>> {
    const focusConnectApi = new StateFixture();
    const getResponse = await focusConnectApi.getStatesByCountry(countryId);
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.StateLocationList), getResponse.data);
    return getResponse;
  }

  public async getState(countryId: string, stateId: string): Promise<AxiosResponse<IState | undefined>> {
    const focusConnectApi = new StateFixture();
    const getResponse = await focusConnectApi.getState(countryId, stateId);
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.StateLocation), getResponse.data);
    return getResponse;
  }

  public async createState(countryId: string, state: IState, expectedStatus: number = 201): Promise<AxiosResponse<IState>> {
    const focusConnectApi = new StateFixture();
    console.log("Creating state " + state.id);
    let createResponse = await focusConnectApi.createState(countryId, state)
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.StateLocation), createResponse.data);
    return createResponse;
  }

  public async updateState(countryId: string, stateId: string, state: IState, expectedStatus: number = 200): Promise<AxiosResponse<IState>> {
    const focusConnectApi = new StateFixture();
    console.log("Updating state " + stateId);
    let updateResponse = await focusConnectApi.updateState(countryId, stateId, state);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.StateLocation), updateResponse.data);
    return updateResponse;
  }

  public async deleteState(countryId: string, stateId: string, expectedStatus: number = 204): Promise<AxiosResponse<any>> {
    const focusConnectApi = new StateFixture();
    console.log("Deleting state " + stateId);
    const delResponse = await focusConnectApi.deleteState(countryId, stateId);
    expect(delResponse.status).to.equal(expectedStatus);
    return delResponse;
  }
}