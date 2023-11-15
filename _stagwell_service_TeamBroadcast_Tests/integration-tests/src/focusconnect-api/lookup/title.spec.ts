import chai, { expect } from "chai";
import chaiAsPromised from "chai-as-promised";
import { TitleHelper } from "./title-helper";
import { LookupHelper } from "./lookup-helper";
import { CosmosDbHelper, ILookupItem } from "../../helpers/cosmos-helper";

chai.use(chaiAsPromised);

const titleHelper = new TitleHelper();
const dbHelper = new CosmosDbHelper();

describe("Title", async function (): Promise<void> {

  before(async function () {
    console.log("Title Before...");
  });

  after(async function () {
    console.log("Title After...");
    //await dbHelper.deleteTestData(false, ["Departments", "PersonnelRoles", "Titles", "Locations"]);
  });

  this.timeout(360000);

  it("Title - Retrieve all", async function (): Promise<void> {
    const createTitleRequest1 = LookupHelper.createTestLookupItem("title");
    await titleHelper.createTitle(createTitleRequest1);

    const createTitleRequest2 = LookupHelper.createTestLookupItem("title");
    await titleHelper.createTitle(createTitleRequest2);

    const retrieveResponse1 = await titleHelper.getTitles();
    const idList1 = retrieveResponse1.data.map(c => c.id);
    expect(idList1).to.contain(createTitleRequest1.id);
    expect(idList1).to.contain(createTitleRequest2.id);

    await titleHelper.deleteTitle(createTitleRequest2.id);

    const retrieveResponse2 = await titleHelper.getTitles();
    const idList2 = retrieveResponse2.data.map(c => c.id);
    expect(idList1).to.contain(createTitleRequest1.id);
    expect(idList2).to.not.contain(createTitleRequest2.id);
  });

  it("Title - Create", async function (): Promise<void> {
    const createTitleRequest = LookupHelper.createTestLookupItem("title");
    const createTitleResponse = await titleHelper.createTitle(createTitleRequest);
    expect(createTitleResponse.data.id).equals(createTitleRequest.id);
    expect(createTitleResponse.data.name).equals(createTitleRequest.name);
    expect(createTitleResponse.data.status).equals("Active");
  });

  it("Title - Update", async function (): Promise<void> {
    // CREATE user
    const user = await dbHelper.createTestUser();

    // GET title
    const title = await dbHelper.getTitle(user.titleId!);
    expect(title).to.not.be.undefined;

    // UPDATE title
    const updatedName = title!.name + " UPDATED";
    const lookupItem: ILookupItem = { id: title!.id, name: updatedName, status: "Active" };
    await titleHelper.updateTitle(title!.id, lookupItem);

    // GET user
    const updatedUser = await dbHelper.getUser(user.id, user.email);
    expect(updatedUser).to.not.be.undefined;
    expect(updatedUser!.titleName).equals(updatedName);
  });

  it("Title - Retrieve", async function (): Promise<void> {
    // CREATE
    const title = await dbHelper.createTestLookup("Titles", "title");

    // RETRIEVE
    const retrieveTitleResponse = await titleHelper.getTitle(title.id);
    expect(retrieveTitleResponse.data).to.not.be.undefined;

    const retrievedTitle = retrieveTitleResponse.data!;
    expect(retrievedTitle.id).equals(title.id);
    expect(retrievedTitle.name).equals(title.name);
    expect(retrievedTitle.status).equals("Active");
    expect(retrievedTitle.activeRelations?.length).equals(1);
    expect(retrievedTitle.activeRelations![0].name).equals("User");
    expect(retrievedTitle.activeRelations![0].activeCount).equals(0);
  });

  it("Title - Retrieve With Relation", async function (): Promise<void> {
    // CREATE title
    const title = await dbHelper.createTestLookup("Titles", "title");

    // CREATE users
    await dbHelper.createTestUser({ title });
    await dbHelper.createTestUser({ title, isInactive: true });

    // RETRIEVE title
    const retrieveTitleResponse = await titleHelper.getTitle(title.id);
    expect(retrieveTitleResponse.data).to.not.be.undefined;

    const retrievedTitle = retrieveTitleResponse.data!;
    expect(retrievedTitle.activeRelations).is.not.undefined;
    expect(retrievedTitle.activeRelations?.length).equals(1);
    expect(retrievedTitle.activeRelations![0].name).equals("User");
    expect(retrievedTitle.activeRelations![0].activeCount).equals(1);
  });

  it("Title - Delete", async function (): Promise<void> {
    // CREATE
    const title = await dbHelper.createTestLookup("Titles", "title");

    // DELETE
    await titleHelper.deleteTitle(title.id);

    // RETRIEVE
    const deletedTitle = await dbHelper.getTitle(title.id);
    expect(deletedTitle?.status).equals("Deleted");
  });

  it("Title - Create, Update, Retrieve and Delete", async function (): Promise<void> {
    // CREATE
    const createTitleRequest = LookupHelper.createTestLookupItem("title");
    const createTitleResponse = await titleHelper.createTitle(createTitleRequest);
    expect(createTitleResponse.data.id).equals(createTitleRequest.id);
    expect(createTitleResponse.data.name).equals(createTitleRequest.name);
    expect(createTitleResponse.data.status).equals(createTitleRequest.status);

    // UPDATE
    createTitleRequest.name = createTitleRequest.name + " - UPDATED";
    const updateTitleResponse = await titleHelper.updateTitle(createTitleRequest.id, createTitleRequest);
    expect(updateTitleResponse.data.id).equals(createTitleRequest.id);
    expect(updateTitleResponse.data.name).equals(createTitleRequest.name);
    expect(updateTitleResponse.data.status).equals(createTitleRequest.status);

    // RETRIEVE
    const retrieveTitleResponse1 = await titleHelper.getTitle(createTitleRequest.id);
    expect(retrieveTitleResponse1.data).to.not.be.undefined;
    expect(retrieveTitleResponse1.data!.id).equals(createTitleRequest.id);
    expect(retrieveTitleResponse1.data!.name).equals(createTitleRequest.name);
    expect(retrieveTitleResponse1.data!.status).equals(createTitleRequest.status);

    // DELETE
    await titleHelper.deleteTitle(createTitleRequest.id);

    // RETRIEVE
    const retrieveTitleResponse2 = await titleHelper.getTitle(createTitleRequest.id);
    expect(retrieveTitleResponse2.data).to.not.be.undefined;
    expect(retrieveTitleResponse2.data!.status).equals("Deleted");
  });

  describe("Title Exceptions", async function (): Promise<void> {
    it("Title - Retrieve - 404 Title Not Found", async function (): Promise<void> {
      await titleHelper.getTitle("UNKNOWN_TITLE_ID", 404);
    });

    it("Title - Create - 409 Title Exists", async function (): Promise<void> {
      const createTitleRequest = LookupHelper.createTestLookupItem("title");
      await titleHelper.createTitle(createTitleRequest);
      await titleHelper.createTitle(createTitleRequest, 409);
    });

    it("Title - Create - 400 Bad Request", async function (): Promise<void> {
      const createTitleRequest: any = {
        idzzz: LookupHelper.createTestGuid(),
        name: "test title",
        status: "Active"
      };
      await titleHelper.createTitle(createTitleRequest, 400);
    });

    it("Title - Update - 404 Title Not Found", async function (): Promise<void> {
      const updateTitleRequest = LookupHelper.createTestLookupItem("title");
      await titleHelper.updateTitle(updateTitleRequest.id, updateTitleRequest, 404);
    });

    it("Title - Update - 400 Bad Request", async function (): Promise<void> {
      const createTitleRequest = LookupHelper.createTestLookupItem("title");
      await titleHelper.createTitle(createTitleRequest);
      const updateTitleRequest: any = {
        id: createTitleRequest.id,
        namezzz: "test title",
        status: "Active"
      };
      await titleHelper.updateTitle(updateTitleRequest.id, updateTitleRequest, 400);
    });

    it("Title - Update - 404 Path Parameter Mismatch", async function (): Promise<void> {
      const createTitleRequest = LookupHelper.createTestLookupItem("title");
      await titleHelper.createTitle(createTitleRequest);
      const updateTitleRequest = {
        id: createTitleRequest.id,
        name: `${createTitleRequest.name} UPDATED`,
        status: "Active"
      };
      await titleHelper.updateTitle("INVALID_TITLE_ID", updateTitleRequest, 404);
    });

    it("Title - Delete - 404 Title Not Found", async function (): Promise<void> {
      await titleHelper.deleteTitle("UNKNOWN_TITLE_ID", 404);
    });

    it("Title - Delete - 409 Title linked to user", async function (): Promise<void> {
      // CREATE title
      const title = await dbHelper.createTestLookup("Titles", "title");

      // CREATE user
      await dbHelper.createTestUser({ title });

      // DELETE department
      await titleHelper.deleteTitle(title.id, 409);
    });
  });
});