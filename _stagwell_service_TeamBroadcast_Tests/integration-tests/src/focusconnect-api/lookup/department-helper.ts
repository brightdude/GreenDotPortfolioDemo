import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { DepartmentFixture } from "./department-fixture";
import { expect } from "chai";
import { AxiosResponse } from "axios";
import { ILookupItem } from "../../helpers/cosmos-helper";

const schemaProvider = new SchemaProvider();

export class DepartmentHelper {
  public async getDepartments(): Promise<AxiosResponse<ILookupItem[]>> {
    const focusConnectApi = new DepartmentFixture();
    const getResponse = await focusConnectApi.getDepartments();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupList), getResponse.data);
    return getResponse;
  }

  public async getDepartment(departmentId: string, expectedStatus: number = 200): Promise<AxiosResponse<ILookupItem | undefined>> {
    const focusConnectApi = new DepartmentFixture();
    const getResponse = await focusConnectApi.getDepartment(departmentId);
    expect(getResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupItem), getResponse.data);
    return getResponse;
  }

  public async createDepartment(dept: ILookupItem, expectedStatus: number = 201): Promise<AxiosResponse<ILookupItem>> {
    const focusConnectApi = new DepartmentFixture();
    console.log("Creating department " + dept.id);
    const createResponse = await focusConnectApi.createDepartment(dept)
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupItem), createResponse.data);
    return createResponse;
  }

  public async updateDepartment(departmentId: string, dept: ILookupItem, expectedStatus: number = 200): Promise<AxiosResponse<ILookupItem>> {
    const focusConnectApi = new DepartmentFixture();
    console.log("Updating department " + departmentId);
    const updateResponse = await focusConnectApi.updateDepartment(departmentId, dept);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupItem), updateResponse.data);
    return updateResponse;
  }

  public async deleteDepartment(departmentId: string, expectedStatus: number = 204): Promise<AxiosResponse<any>> {
    const focusConnectApi = new DepartmentFixture();
    console.log("Deleting department " + departmentId);
    const delResponse = await focusConnectApi.deleteDepartment(departmentId);
    expect(delResponse.status).to.equal(expectedStatus);
    return delResponse;
  }
}