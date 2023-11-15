import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { PersonnelRoleFixture } from "./personnel-role-fixture";
import { expect } from "chai";
import { AxiosResponse } from "axios";
import { ILookupItem } from "../../helpers/cosmos-helper";

const schemaProvider = new SchemaProvider();

export class PersonnelRoleHelper {

  public async getPersonnelRoles(): Promise<AxiosResponse<ILookupItem[]>> {
    const focusConnectApi = new PersonnelRoleFixture();
    const getResponse = await focusConnectApi.getPersonnelRoles();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupList), getResponse.data);
    return getResponse;
  }

  public async getPersonnelRole(personnelRoleId: string, expectedStatus: number = 200): Promise<AxiosResponse<ILookupItem | undefined>> {
    const focusConnectApi = new PersonnelRoleFixture();
    const getResponse = await focusConnectApi.getPersonnelRole(personnelRoleId);
    expect(getResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupItem), getResponse.data);
    return getResponse;
  }

  public async createPersonnelRole(role: ILookupItem, expectedStatus: number = 201): Promise<AxiosResponse<ILookupItem>> {
    const focusConnectApi = new PersonnelRoleFixture();
    console.log("Creating personnel role " + role.id);
    const createResponse = await focusConnectApi.createPersonnelRole(role)
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupItem), createResponse.data);
    return createResponse;
  }

  public async updatePersonnelRole(personnelRoleId: string, role: ILookupItem, expectedStatus: number = 200): Promise<AxiosResponse<ILookupItem>> {
    const focusConnectApi = new PersonnelRoleFixture();
    console.log("Updating personnel role " + personnelRoleId);
    const updateResponse = await focusConnectApi.updatePersonnelRole(personnelRoleId, role);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupItem), updateResponse.data);
    return updateResponse;
  }

  public async deletePersonnelRole(personnelRoleId: string, expectedStatus: number = 204): Promise<AxiosResponse<any>> {
    const focusConnectApi = new PersonnelRoleFixture();
    console.log("Deleting personnel role " + personnelRoleId);
    const delResponse = await focusConnectApi.deletePersonnelRole(personnelRoleId);
    expect(delResponse.status).to.equal(expectedStatus);
    return delResponse;
  }
}