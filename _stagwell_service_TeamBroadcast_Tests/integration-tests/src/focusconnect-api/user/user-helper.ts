import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { UserFixture } from "./user-fixture";
import { expect } from "chai";
import { AxiosResponse } from "axios";
import { CosmosDbHelper, ILookupItem, IUser, IUserCreateUpdateParams } from "../../helpers/cosmos-helper";
import { LookupHelper } from "../lookup/lookup-helper";

const schemaProvider = new SchemaProvider();
const userApi = new UserFixture();

export class UserHelper {
  public async getUsers(): Promise<AxiosResponse<IUser[]>> {
    const getResponse = await userApi.getUsers();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.UserList), getResponse.data);
    return getResponse;
  }

  public async getUser(email: string, expectedStatus: number = 200): Promise<AxiosResponse<IUser | undefined>> {
    const getResponse = await userApi.getUser(email);
    expect(getResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.User), getResponse.data);
    return getResponse;
  }

  public async createUser(user: IUserCreateUpdateParams, expectedStatus: number = 201): Promise<AxiosResponse<IUser>> {
    const updateResponse = await userApi.createUser(user);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.User), updateResponse.data);
    return updateResponse;
  }

  public async updateUser(emailAddress: string, user: IUserCreateUpdateParams, expectedStatus: number = 200): Promise<AxiosResponse<IUser>> {
    const updateResponse = await userApi.updateUser(emailAddress, user);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.User), updateResponse.data);
    return updateResponse;
  }

  public async deleteUser(email: string, expectedStatus: number = 204): Promise<any> {
    const deleteResponse = await userApi.deleteUser(email);
    expect(deleteResponse.status).to.equal(expectedStatus);
    return deleteResponse;
  }

  public async createUserRequestBody(options?: { email?: string, title?: ILookupItem, role?: ILookupItem, department?: ILookupItem }): Promise<IUserCreateUpdateParams> {
    const email = options?.email ?? LookupHelper.createTestEmail();
    const dbHelper = new CosmosDbHelper();
    const role = options?.role ?? await dbHelper.createTestLookup("PersonnelRoles", "personnel role");
    const department = options?.department ?? await dbHelper.createTestLookup("Departments", "department");
    return {
      email,
      displayName: `__test_Test User`,
      firstName: "__test_Test",
      lastName: "__test_User",
      titleId: options?.title?.id,
      roleId: role.id,
      departmentId: department.id,
      phone: "123-123-123-123",
      accessLevel: "tier2"
    };
  }
}