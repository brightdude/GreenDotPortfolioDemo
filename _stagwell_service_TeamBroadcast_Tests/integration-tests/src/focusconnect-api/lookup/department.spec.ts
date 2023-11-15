import chai, { expect } from "chai";
import chaiAsPromised from "chai-as-promised";
import { CosmosDbHelper, ILookupItem } from "../../helpers/cosmos-helper";
import { DepartmentHelper } from "./department-helper";
import { LookupHelper } from "./lookup-helper";

chai.use(chaiAsPromised);

const departmentHelper = new DepartmentHelper();
const dbHelper = new CosmosDbHelper();

describe("Department", async function (): Promise<void> {

  before(async function () {
    console.log("Department Before...");
  });

  after(async function () {
    console.log("Department After...");
    //await dbHelper.deleteTestData(false, ["Departments", "PersonnelRoles", "Titles", "Locations"]);
  });

  this.timeout(360000);

  it("Department - Retrieve all", async function (): Promise<void> {
    const createDepartmentRequest1 = LookupHelper.createTestLookupItem("department");
    await departmentHelper.createDepartment(createDepartmentRequest1);

    const createDepartmentRequest2 = LookupHelper.createTestLookupItem("department");
    await departmentHelper.createDepartment(createDepartmentRequest2);

    const retrieveResponse1 = await departmentHelper.getDepartments();
    const idList1 = retrieveResponse1.data.map(c => c.id);
    expect(idList1).to.contain(createDepartmentRequest1.id);
    expect(idList1).to.contain(createDepartmentRequest2.id);

    await departmentHelper.deleteDepartment(createDepartmentRequest2.id);

    const retrieveResponse2 = await departmentHelper.getDepartments();
    const idList2 = retrieveResponse2.data.map(c => c.id);
    expect(idList1).to.contain(createDepartmentRequest1.id);
    expect(idList2).to.not.contain(createDepartmentRequest2.id);
  });

  it("Department - Create", async function (): Promise<void> {
    // CREATE
    const createDepartmentRequest = LookupHelper.createTestLookupItem("department");
    const createDepartmentResponse = await departmentHelper.createDepartment(createDepartmentRequest);
    expect(createDepartmentResponse.data.id).equals(createDepartmentRequest.id);
    expect(createDepartmentResponse.data.name).equals(createDepartmentRequest.name);
    expect(createDepartmentResponse.data.status).equals("Active");
  });

  it("Department - Update", async function (): Promise<void> {
    // CREATE department
    const department = await dbHelper.createTestLookup("Departments", "department");

    // CREATE user
    const user = await dbHelper.createTestUser({ department });

    // CREATE calendar
    const calendar = await dbHelper.createTestCalendar({ department });

    // UPDATE department
    const updatedName = department!.name + " UPDATED";
    const lookupItem: ILookupItem = { id: department!.id, name: updatedName, status: "Active" };
    const updateResponse = await departmentHelper.updateDepartment(department!.id, lookupItem);
    expect(updateResponse.data.name).equals(updatedName);

    // GET user
    const updatedUser = await dbHelper.getUser(user.id, user.email);
    expect(updatedUser).to.not.be.undefined;
    expect(updatedUser!.departmentName).equals(updatedName);

    // GET calendar
    const updatedCalendar = await dbHelper.getCalendar(calendar.externalCalendarId);
    expect(updatedCalendar).to.not.be.undefined;
    expect(updatedCalendar!.departmentName).equals(updatedName);
  });

  it("Department - Retrieve", async function (): Promise<void> {
    // CREATE
    const department = await dbHelper.createTestLookup("Departments", "department");

    // RETRIEVE
    const retrieveDepartmentResponse = await departmentHelper.getDepartment(department.id);
    expect(retrieveDepartmentResponse.data).to.not.be.undefined;

    const retrievedDepartment = retrieveDepartmentResponse.data!;
    expect(retrievedDepartment.id).equals(department.id);
    expect(retrievedDepartment.name).equals(department.name);
    expect(retrievedDepartment.status).equals("Active");
    expect(retrievedDepartment.activeRelations?.length).equals(2);
    expect(retrievedDepartment.activeRelations!.map(r => r.name)).includes("User");
    expect(retrievedDepartment.activeRelations!.map(r => r.name)).includes("Calendar");
    expect(retrievedDepartment.activeRelations![0].activeCount).equals(0);
    expect(retrievedDepartment.activeRelations![1].activeCount).equals(0);
  });

  it("Department - Retrieve With Relation", async function (): Promise<void> {
    // CREATE department
    const department = await dbHelper.createTestLookup("Departments", "department");

    // CREATE calendar
    await dbHelper.createTestCalendar({ department });

    // CREATE users
    await dbHelper.createTestUser({ department });
    await dbHelper.createTestUser({ department, isInactive: true });

    // RETRIEVE department
    const retrieveDepartmentResponse = await departmentHelper.getDepartment(department.id);
    expect(retrieveDepartmentResponse.data).to.not.be.undefined;

    const retrievedDepartment = retrieveDepartmentResponse.data!;
    expect(retrievedDepartment.activeRelations).is.not.undefined;
    expect(retrievedDepartment.activeRelations!.length).equals(2);
    const userRelation = retrievedDepartment.activeRelations!.filter(r => r.name === "User").pop();
    expect(userRelation).is.not.undefined;
    expect(userRelation!.activeCount).equals(1);
    const calendarRelation = retrievedDepartment.activeRelations!.filter(r => r.name === "Calendar").pop();
    expect(calendarRelation).is.not.undefined;
    expect(calendarRelation!.activeCount).equals(1);
  });

  it("Department - Delete", async function (): Promise<void> {
    // CREATE
    const department = await dbHelper.createTestLookup("Departments", "department");

    // DELETE
    await departmentHelper.deleteDepartment(department.id);

    // RETRIEVE
    const deletedDepartment = await dbHelper.getDepartment(department.id);
    expect(deletedDepartment?.status).equals("Deleted");
  });

  it("Department - Create, Update, Retrieve and Delete", async function (): Promise<void> {
    // CREATE
    const createDepartmentRequest = LookupHelper.createTestLookupItem("department");
    const createDepartmentResponse = await departmentHelper.createDepartment(createDepartmentRequest);
    expect(createDepartmentResponse.data.id).equals(createDepartmentRequest.id);
    expect(createDepartmentResponse.data.name).equals(createDepartmentRequest.name);
    expect(createDepartmentResponse.data.status).equals(createDepartmentRequest.status);

    // UPDATE
    createDepartmentRequest.name = createDepartmentRequest.name + " - UPDATED";
    const updateDepartmentResponse = await departmentHelper.updateDepartment(createDepartmentRequest.id, createDepartmentRequest);
    expect(updateDepartmentResponse.data.id).equals(createDepartmentRequest.id);
    expect(updateDepartmentResponse.data.name).equals(createDepartmentRequest.name);
    expect(updateDepartmentResponse.data.status).equals(createDepartmentRequest.status);

    // RETRIEVE
    const retrieveDepartmentResponse1 = await departmentHelper.getDepartment(createDepartmentRequest.id);
    expect(retrieveDepartmentResponse1.data).to.not.be.undefined;
    expect(retrieveDepartmentResponse1.data!.id).equals(createDepartmentRequest.id);
    expect(retrieveDepartmentResponse1.data!.name).equals(createDepartmentRequest.name);
    expect(retrieveDepartmentResponse1.data!.status).equals(createDepartmentRequest.status);

    // DELETE
    await departmentHelper.deleteDepartment(createDepartmentRequest.id);

    // RETRIEVE
    const retrieveDepartmentResponse2 = await departmentHelper.getDepartment(createDepartmentRequest.id);
    expect(retrieveDepartmentResponse2.data).to.not.be.undefined;
    expect(retrieveDepartmentResponse2.data!.status).equals("Deleted");
  });

  describe("Department Exceptions", async function (): Promise<void> {
    it("Department - Retrieve - 404 Department Not Found", async function (): Promise<void> {
      await departmentHelper.getDepartment("UNKNOWN_DEPARTMENT_ID", 404);
    });

    it("Department - Create - 409 Department Exists", async function (): Promise<void> {
      const createDepartmentRequest = LookupHelper.createTestLookupItem("department");
      await departmentHelper.createDepartment(createDepartmentRequest);
      await departmentHelper.createDepartment(createDepartmentRequest, 409);
    });

    it("Department - Create - 400 Bad Request", async function (): Promise<void> {
      const createDepartmentRequest: any = {
        idzzz: LookupHelper.createTestGuid(),
        name: "test department",
        status: "Active"
      };
      await departmentHelper.createDepartment(createDepartmentRequest, 400);
    });

    it("Department - Update - 404 Department Not Found", async function (): Promise<void> {
      const updateDepartmentRequest = LookupHelper.createTestLookupItem("department");
      await departmentHelper.updateDepartment(updateDepartmentRequest.id, updateDepartmentRequest, 404);
    });

    it("Department - Update - 400 Bad Request", async function (): Promise<void> {
      const createDepartmentRequest = LookupHelper.createTestLookupItem("department");
      await departmentHelper.createDepartment(createDepartmentRequest);
      const updateDepartmentRequest: any = {
        id: createDepartmentRequest.id,
        namezzz: "test department",
        status: "Active"
      };
      await departmentHelper.updateDepartment(updateDepartmentRequest.id, updateDepartmentRequest, 400);
    });

    it("Department - Update - 404 Path Parameter Mismatch", async function (): Promise<void> {
      const createDepartmentRequest = LookupHelper.createTestLookupItem("department");
      await departmentHelper.createDepartment(createDepartmentRequest);
      const updateDepartmentRequest = {
        id: createDepartmentRequest.id,
        name: `${createDepartmentRequest.name} UPDATED`,
        status: "Active"
      };
      await departmentHelper.updateDepartment("INVALID_DEPARTMENT_ID", updateDepartmentRequest, 404);
    });

    it("Department - Delete - 404 Department Not Found", async function (): Promise<void> {
      await departmentHelper.deleteDepartment("UNKNOWN_DEPARTMENT_ID", 404);
    });

    it("Department - Delete - 409 Department linked to user", async function (): Promise<void> {
      // CREATE department
      const department = await dbHelper.createTestLookup("Departments", "department");

      // CREATE user
      await dbHelper.createTestUser({ department });

      // DELETE department
      await departmentHelper.deleteDepartment(department.id, 409);
    });

    it("Department - Delete - 409 Department linked to calendar", async function (): Promise<void> {
      // CREATE department
      const department = await dbHelper.createTestLookup("Departments", "department");

      // CREATE calendar
      await dbHelper.createTestCalendar({ department });

      // DELETE department
      await departmentHelper.deleteDepartment(department.id, 409);
    });
  });
});