import chai, { expect } from "chai";
import chaiAsPromised from "chai-as-promised";
import { PersonnelRoleHelper } from "./personnel-role-helper";
import { LookupHelper } from "./lookup-helper";
import { CosmosDbHelper, ILookupItem } from "../../helpers/cosmos-helper";

chai.use(chaiAsPromised);

const personnelRoleHelper = new PersonnelRoleHelper();
const dbHelper = new CosmosDbHelper();

describe("Personnel Role", async function (): Promise<void> {

  before(async function () {
    console.log("Personnel Role Before...");
  });

  after(async function () {
    console.log("Personnel Role After...");
    //await dbHelper.deleteTestData(false, ["Departments", "PersonnelRoles", "Titles", "Locations"]);
  });

  this.timeout(360000);

  it("Personnel Role - Retrieve all", async function (): Promise<void> {
    const createPersonnelRoleRequest1 = LookupHelper.createTestLookupItem("personnelRole");
    await personnelRoleHelper.createPersonnelRole(createPersonnelRoleRequest1);

    const createPersonnelRoleRequest2 = LookupHelper.createTestLookupItem("personnelRole");
    await personnelRoleHelper.createPersonnelRole(createPersonnelRoleRequest2);

    const retrieveResponse1 = await personnelRoleHelper.getPersonnelRoles();
    const idList1 = retrieveResponse1.data.map(c => c.id);
    expect(idList1).to.contain(createPersonnelRoleRequest1.id);
    expect(idList1).to.contain(createPersonnelRoleRequest2.id);

    await personnelRoleHelper.deletePersonnelRole(createPersonnelRoleRequest2.id);

    const retrieveResponse2 = await personnelRoleHelper.getPersonnelRoles();
    const idList2 = retrieveResponse2.data.map(c => c.id);
    expect(idList1).to.contain(createPersonnelRoleRequest1.id);
    expect(idList2).to.not.contain(createPersonnelRoleRequest2.id);
  });

  it("Personnel Role - Create", async function (): Promise<void> {
    // CREATE
    const createPersonnelRoleRequest = LookupHelper.createTestLookupItem("personnelRole");
    const createPersonnelRoleResponse = await personnelRoleHelper.createPersonnelRole(createPersonnelRoleRequest);
    expect(createPersonnelRoleResponse.data.id).equals(createPersonnelRoleRequest.id);
    expect(createPersonnelRoleResponse.data.name).equals(createPersonnelRoleRequest.name);
    expect(createPersonnelRoleResponse.data.status).equals("Active");
  });

  it("Personnel Role - Update", async function (): Promise<void> {
    // CREATE user
    const user = await dbHelper.createTestUser();

    // GET role
    const role = await dbHelper.getRole(user.roleId!);
    expect(role).to.not.be.undefined;

    // UPDATE role
    const updatedName = role!.name + " UPDATED";
    const lookupItem: ILookupItem = { id: role!.id, name: updatedName, status: "Active" };
    const updateResponse = await personnelRoleHelper.updatePersonnelRole(role!.id, lookupItem);
    expect(updateResponse.data.name).equals(updatedName);

    // GET user
    const updatedUser = await dbHelper.getUser(user.id, user.email);
    expect(updatedUser).to.not.be.undefined;
    expect(updatedUser!.roleName).equals(updatedName);
  });

  it("Personnel Role - Retrieve", async function (): Promise<void> {
    // CREATE
    const role = await dbHelper.createTestLookup("PersonnelRoles", "role");

    // RETRIEVE
    const retrieveRoleResponse = await personnelRoleHelper.getPersonnelRole(role.id);
    expect(retrieveRoleResponse.data).to.not.be.undefined;

    const retrievedRole = retrieveRoleResponse.data!;
    expect(retrievedRole.id).equals(role.id);
    expect(retrievedRole.name).equals(role.name);
    expect(retrievedRole.status).equals("Active");
    expect(retrievedRole.activeRelations?.length).equals(1);
    expect(retrievedRole.activeRelations![0].name).equals("User");
    expect(retrievedRole.activeRelations![0].activeCount).equals(0);
  });

  it("Personnel Role - Retrieve With Relation", async function (): Promise<void> {
    // CREATE role
    const role = await dbHelper.createTestLookup("PersonnelRoles", "role");

    // CREATE users
    await dbHelper.createTestUser({ role });
    await dbHelper.createTestUser({ role, isInactive: true });

    // RETRIEVE role
    const retrieveRoleResponse = await personnelRoleHelper.getPersonnelRole(role.id);
    expect(retrieveRoleResponse.data).to.not.be.undefined;

    const retrievedRole = retrieveRoleResponse.data!;
    expect(retrievedRole.activeRelations).is.not.undefined;
    expect(retrievedRole.activeRelations?.length).equals(1);
    expect(retrievedRole.activeRelations![0].name).equals("User");
    expect(retrievedRole.activeRelations![0].activeCount).equals(1);
  });

  it("Personnel Role - Delete", async function (): Promise<void> {
    // CREATE
    const role = await dbHelper.createTestLookup("PersonnelRoles", "role");

    // DELETE
    await personnelRoleHelper.deletePersonnelRole(role.id);

    // RETRIEVE
    const deletedRole = await dbHelper.getRole(role.id);
    expect(deletedRole?.status).equals("Deleted");
  });

  it("Personnel Role - Create, Update, Retrieve and Delete", async function (): Promise<void> {
    // CREATE
    const createPersonnelRoleRequest = LookupHelper.createTestLookupItem("personnelRole");
    const createPersonnelRoleResponse = await personnelRoleHelper.createPersonnelRole(createPersonnelRoleRequest);
    expect(createPersonnelRoleResponse.data.id).equals(createPersonnelRoleRequest.id);
    expect(createPersonnelRoleResponse.data.name).equals(createPersonnelRoleRequest.name);
    expect(createPersonnelRoleResponse.data.status).equals(createPersonnelRoleRequest.status);

    // UPDATE
    createPersonnelRoleRequest.name = createPersonnelRoleRequest.name + " - UPDATED";
    const updatePersonnelRoleResponse = await personnelRoleHelper.updatePersonnelRole(createPersonnelRoleRequest.id, createPersonnelRoleRequest);
    expect(updatePersonnelRoleResponse.data.id).equals(createPersonnelRoleRequest.id);
    expect(updatePersonnelRoleResponse.data.name).equals(createPersonnelRoleRequest.name);
    expect(updatePersonnelRoleResponse.data.status).equals(createPersonnelRoleRequest.status);

    // RETRIEVE
    const retrievePersonnelRoleResponse1 = await personnelRoleHelper.getPersonnelRole(createPersonnelRoleRequest.id);
    expect(retrievePersonnelRoleResponse1.data).to.not.be.undefined;
    expect(retrievePersonnelRoleResponse1.data!.id).equals(createPersonnelRoleRequest.id);
    expect(retrievePersonnelRoleResponse1.data!.name).equals(createPersonnelRoleRequest.name);
    expect(retrievePersonnelRoleResponse1.data!.status).equals(createPersonnelRoleRequest.status);

    // DELETE
    await personnelRoleHelper.deletePersonnelRole(createPersonnelRoleRequest.id);

    // RETRIEVE
    const retrievePersonnelRoleResponse2 = await personnelRoleHelper.getPersonnelRole(createPersonnelRoleRequest.id);
    expect(retrievePersonnelRoleResponse2.data).to.not.be.undefined;
    expect(retrievePersonnelRoleResponse2.data!.status).equals("Deleted");
  });

  describe("Personnel Role Exceptions", async function (): Promise<void> {
    it("Personnel Role - Retrieve - 404 Personnel Role Not Found", async function (): Promise<void> {
      await personnelRoleHelper.getPersonnelRole("UNKNOWN_PERSONNEL_ROLE_ID", 404);
    });

    it("Personnel Role - Create - 409 Personnel Role Exists", async function (): Promise<void> {
      const createPersonnelRoleRequest = LookupHelper.createTestLookupItem("personnelRole");
      await personnelRoleHelper.createPersonnelRole(createPersonnelRoleRequest);
      await personnelRoleHelper.createPersonnelRole(createPersonnelRoleRequest, 409);
    });

    it("Personnel Role - Create - 400 Bad Request", async function (): Promise<void> {
      const createPersonnelRoleRequest: any = {
        idzzz: LookupHelper.createTestGuid(),
        name: "test personnel role",
        status: "Active"
      };
      await personnelRoleHelper.createPersonnelRole(createPersonnelRoleRequest, 400);
    });

    it("Personnel Role - Update - 404 Personnel Role Not Found", async function (): Promise<void> {
      const updatePersonnelRoleRequest = LookupHelper.createTestLookupItem("personnelRole");
      await personnelRoleHelper.updatePersonnelRole(updatePersonnelRoleRequest.id, updatePersonnelRoleRequest, 404);
    });

    it("Personnel Role - Update - 400 Bad Request", async function (): Promise<void> {
      const createPersonnelRoleRequest = LookupHelper.createTestLookupItem("personnelRole");
      await personnelRoleHelper.createPersonnelRole(createPersonnelRoleRequest);
      const updatePersonnelRoleRequest: any = {
        id: createPersonnelRoleRequest.id,
        namezzz: "test personnel role",
        status: "Active"
      };
      await personnelRoleHelper.updatePersonnelRole(updatePersonnelRoleRequest.id, updatePersonnelRoleRequest, 400);
    });

    it("Personnel Role - Update - 404 Path Parameter Mismatch", async function (): Promise<void> {
      const createPersonnelRoleRequest = LookupHelper.createTestLookupItem("personnelRole");
      await personnelRoleHelper.createPersonnelRole(createPersonnelRoleRequest);
      const updatePersonnelRoleRequest = {
        id: createPersonnelRoleRequest.id,
        name: `${createPersonnelRoleRequest.name} UPDATED`,
        status: "Active"
      };
      await personnelRoleHelper.updatePersonnelRole("INVALID_PERSONNEL_ROLE_ID", updatePersonnelRoleRequest, 404);
    });

    it("Personnel Role - Delete - 404 Personnel Role Not Found", async function (): Promise<void> {
      await personnelRoleHelper.deletePersonnelRole("UNKNOWN_PERSONNEL_ROLE_ID", 404);
    });

    it("Personnel Role - Delete - 409 Personnel Role linked to user", async function (): Promise<void> {
      // CREATE personnel role
      const role = await dbHelper.createTestLookup("PersonnelRoles", "role");

      // CREATE user
      await dbHelper.createTestUser({ role });

      // DELETE department
      await personnelRoleHelper.deletePersonnelRole(role.id, 409);
    });
  });
});