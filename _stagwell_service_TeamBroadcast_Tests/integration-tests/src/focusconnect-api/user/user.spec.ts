import { expect } from "chai";
import { CosmosDbHelper, IUser, IUserCreateUpdateParams } from "../../helpers/cosmos-helper";
import { GraphHelper } from "../../helpers/graph-helper";
import { KeyvaultHelper } from "../../helpers/keyvault-helper";
import { TenantSettingsHelper } from "../../helpers/tenant-settings-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { UserHelper } from "./user-helper";

const userHelper = new UserHelper();
const dbHelper = new CosmosDbHelper();

describe("User", async function (): Promise<void> {

    before(async function () {
        console.log("User Before...");
    });

    after(async function () {
        console.log("User After...");
        //await dbHelper.deleteTestData(false, ["Departments", "PersonnelRoles", "Titles", "Locations"]);
    });

    this.timeout(360000);

    it("User - Retrieve all active users", async function (): Promise<void> {
        // CREATE
        const user1 = await dbHelper.createTestUser();
        const user2 = await dbHelper.createTestUser();

        // GET
        const getResponse1 = await userHelper.getUsers();
        const userIdList1 = getResponse1.data.map(u => u.id);
        expect(userIdList1).to.contain(user1.id);
        expect(userIdList1).to.contain(user2.id);

        // Flag user as inactive
        user2.activeFlag = false;
        await dbHelper.updateUser(user2);

        // GET
        const getResponse2 = await userHelper.getUsers();
        const userIdList2 = getResponse2.data.map(u => u.id);
        expect(userIdList2).to.contain(user1.id);
        expect(userIdList2).to.not.contain(user2.id);
    });

    it("User - Retrieve", async function (): Promise<void> {
        // CREATE
        const user = await dbHelper.createTestUser();

        // GET
        const retrieveResponse = await userHelper.getUser(user.email);
        expect(retrieveResponse.data).is.not.undefined;
        const retrievedUser = retrieveResponse.data!;
        expect(retrievedUser.id).equals(user.id);
        expect(retrievedUser.email).equals(user.email);
        expect(retrievedUser.activeFlag).is.true;

        // GET with case change
        await userHelper.getUser((user.email as string).toUpperCase());
    });

    it("User - Create", async function (): Promise<void> {
        // CREATE lookups
        const role = await dbHelper.createTestLookup("PersonnelRoles", "personnel role");
        const department = await dbHelper.createTestLookup("Departments", "department");

        // CREATE user
        const email = LookupHelper.createTestEmail();
        const createResponse = await userHelper.createUser(await userHelper.createUserRequestBody({ email, role: role, department: department }));
        expect(createResponse.data).is.not.undefined;
        const createdUser = createResponse.data;
        expect(createdUser.email).equals(email);
        expect(createdUser.departmentName).equals(department.name);
        expect(createdUser.roleName).equals(role.name);

        // GET user
        const retrievedUser = (await dbHelper.getUser(createdUser.id, email))!;
        expect(retrievedUser).is.not.undefined;
        expect(retrievedUser.id).equals(createdUser.id);
        expect(retrievedUser.email).equals(email);
        expect(retrievedUser.departmentName).equals(department.name);
        expect(retrievedUser.roleName).equals(role.name);
        expect(retrievedUser.msAadId).is.not.undefined;
        expect(retrievedUser.inviteRedeemUrl).is.not.undefined;
        const defaultUsageLocation = await TenantSettingsHelper.getDefaultUsageLocation();
        expect(retrievedUser.usageLocation).equals(defaultUsageLocation);

        // GET ms user
        const graphHelper = new GraphHelper();
        const msUser = (await graphHelper.UserGet(createdUser.msAadId!))!;
        expect(msUser.id).equals(retrievedUser.msAadId);
        expect(msUser.mail).equals(email);
        expect(msUser.displayName).equals(retrievedUser.displayName);
        expect(msUser.givenName).equals(retrievedUser.firstName);
        expect(msUser.surname).equals(retrievedUser.lastName);

        // Pause for 30 seconds before requesting team members
        await new Promise(resolve => setTimeout(resolve, 30000));

        // GET everyone team members
        const keyvaultHelper = new KeyvaultHelper();
        const members = await graphHelper.TeamMembersList(await keyvaultHelper.GetEveryoneTeamId());
        expect(members.map(m => m.email)).includes(email);
    });

    it("User - Update", async function (): Promise<void> {
        // CREATE department
        const dept1 = await dbHelper.createTestLookup("Departments", "department");

        // CREATE user
        const user = await dbHelper.createTestUser({ createMsUser: true, department: dept1 });

        // CREATE different department
        const dept2 = await dbHelper.createTestLookup("Departments", "department");

        // UPDATE user
        const updatedFirstName = user.firstName + " UPDATED";
        const updatedLastName = user.lastName + " UPDATED";
        const updatedDisplayName = user.displayName + " UPDATED";
        const updateRequest: IUserCreateUpdateParams = {
            accessLevel: user.accessLevel,
            departmentId: dept2.id,
            displayName: updatedDisplayName,
            email: user.email,
            firstName: updatedFirstName,
            lastName: updatedLastName,
            roleId: user.roleId
        };
        const updateResponse = await userHelper.updateUser(user.email, updateRequest);
        expect(updateResponse.data).is.not.undefined;

        const updatedUser = updateResponse.data;
        expect(updatedUser.id).equals(user.id);
        expect(updatedUser.email).equals(user.email);
        expect(updatedUser.firstName).equals(updatedFirstName);
        expect(updatedUser.lastName).equals(updatedLastName);
        expect(updatedUser.displayName).equals(updatedDisplayName);
        expect(updatedUser.departmentName).equals(dept2.name);

        // GET user
        const retrievedUser = (await dbHelper.getUser(user.id, user.email))!;
        expect(retrievedUser.id).equals(user.id);
        expect(retrievedUser.email).equals(user.email);
        expect(retrievedUser.roleName).equals(user.roleName);
        expect(retrievedUser.accessLevel).equals(user.accessLevel);
        expect(retrievedUser.firstName).equals(updatedFirstName);
        expect(retrievedUser.lastName).equals(updatedLastName);
        expect(retrievedUser.displayName).equals(updatedDisplayName);
        expect(retrievedUser.departmentName).equals(dept2.name);

        // GET ms user
        const graphHelper = new GraphHelper();
        const msUser = await graphHelper.UserGet(retrievedUser.msAadId!);
        expect(msUser).is.not.undefined;
        expect(msUser?.givenName).equals(updatedFirstName);
        expect(msUser?.surname).equals(updatedLastName);
        expect(msUser?.displayName).equals(updatedDisplayName);
    });

    it("User - Update - deleted user", async function (): Promise<void> {
        // CREATE user
        const user = await dbHelper.createTestUser({ createMsUser: true });

        // Pause for 30 seconds
        await new Promise(resolve => setTimeout(resolve, 30000));

        // DELETE user
        await userHelper.deleteUser(user.email);

        // Pause for 30 seconds
        await new Promise(resolve => setTimeout(resolve, 30000));

        // GET ms user
        const graphHelper = new GraphHelper();
        await graphHelper.UserGet(user.msAadId!, 404);

        // UPDATE user
        const updatedFirstName = user.firstName + " UPDATED";
        const updatedLastName = user.lastName + " UPDATED";
        const updatedDisplayName = user.displayName + " UPDATED";
        const updateRequest: IUserCreateUpdateParams = {
            accessLevel: user.accessLevel,
            departmentId: user.departmentId,
            displayName: updatedDisplayName,
            email: user.email,
            firstName: updatedFirstName,
            lastName: updatedLastName,
            roleId: user.roleId
        };
        const updateResponse = await userHelper.updateUser(user.email, updateRequest);

        // GET user
        const updatedUser = await dbHelper.getUser(user.id, user.email);
        expect(updatedUser?.firstName).equals(updatedFirstName);
        expect(updatedUser?.lastName).equals(updatedLastName);
        expect(updatedUser?.displayName).equals(updatedDisplayName);
        expect(updatedUser?.msAadId).is.not.undefined;

        // GET ms user again
        const msUser = await graphHelper.UserGet(updateResponse.data.msAadId!);
        expect(msUser).is.not.undefined;
        expect(msUser?.id).equals(updatedUser?.msAadId);
        expect(msUser?.givenName).equals(updatedFirstName);
        expect(msUser?.surname).equals(updatedLastName);
        expect(msUser?.displayName).equals(updatedDisplayName);
    });

    it("User - Update - access level change", async function (): Promise<void> {
        // CREATE user
        const user = await dbHelper.createTestUser({ createMsUser: true });

        // CREATE facility
        const facility = await dbHelper.createTestFacility({ createTeam: true });
        const meetingsChannelId = facility.team.channels.filter(c => c.name === "Meetings")[0].msChannelId;

        // CREATE calender
        await dbHelper.createTestCalendar({ facility, focusUsers: [user], addUsersToTeam: true });

        // Pause for 30 seconds before requesting channel members
        await new Promise(resolve => setTimeout(resolve, 30000));

        // User access level is tier2 so should be in Meetings channel
        const graphHelper = new GraphHelper();
        let members = await graphHelper.TeamChannelMembersList(facility.team.msTeamId, meetingsChannelId);
        expect(members.map(m => m.email)).includes(user.email);

        // UPDATE user accessLevel
        const updateRequest: IUserCreateUpdateParams = {
            accessLevel: "tier3",
            departmentId: user.departmentId,
            displayName: user.displayName,
            email: user.email,
            firstName: user.firstName,
            lastName: user.lastName,
            roleId: user.roleId
        }
        const updateResponse = await userHelper.updateUser(user.email, updateRequest);
        expect(updateResponse.data).is.not.undefined;
        const udpatedUser = updateResponse.data;
        expect(udpatedUser.accessLevel).equals("tier3");

        // Pause for 30 seconds before requesting channel members
        await new Promise(resolve => setTimeout(resolve, 30000));

        // User access level is tier3 so should no longer be in Meetings channel
        members = await graphHelper.TeamChannelMembersList(facility.team.msTeamId, meetingsChannelId);
        expect(members.map(m => m.email)).does.not.include(user.email);
    });

    it("User - Delete", async function (): Promise<void> {
        // CREATE user
        const user = await dbHelper.createTestUser({ createMsUser: true });
        expect(user.activeFlag).is.true;

        // GET ms user
        const graphHelper = new GraphHelper();
        const msUser = (await graphHelper.UserGet(user.msAadId!))!;
        expect(msUser.id).equals(user.msAadId);
        expect(msUser.mail).equals(user.email);

        // CREATE facility
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE calendar
        let calendar = await dbHelper.createTestCalendar({ facility, focusUsers: [user], addUsersToTeam: true });
        expect(calendar.focusUsers).includes(user.email);

        // Pause for 30 seconds before requesting team members
        await new Promise(resolve => setTimeout(resolve, 30000));

        // GET calendar assignment
        let teamMembers = await graphHelper.TeamMembersList(facility.team.msTeamId);
        expect(teamMembers.map(m => m.email)).includes(user.email);

        // Pause for 30 seconds before deleting user
        await new Promise(resolve => setTimeout(resolve, 30000));

        // DELETE user
        await userHelper.deleteUser(user.email);

        // Pause for 30 seconds before requesting user
        await new Promise(resolve => setTimeout(resolve, 30000));

        // GET user
        const deletedUser = await dbHelper.getUser(user.id, user.email);
        expect(deletedUser?.activeFlag).is.false;

        // GET ms user again
        await graphHelper.UserGet(user.msAadId!, 404);

        // GET calendar
        calendar = (await dbHelper.getCalendar(calendar.externalCalendarId))!;
        expect(calendar.focusUsers).does.not.include(user.email);

        // GET calendar assignment again
        teamMembers = await graphHelper.TeamMembersList(facility.team.msTeamId);
        expect(teamMembers.map(m => m.email)).does.not.include(user.email);
    });

    it("User - Delete inactive", async function (): Promise<void> {
        // CREATE user
        const user = await dbHelper.createTestUser();

        // DELETE user
        await userHelper.deleteUser(user.email);

        // DELETE user again
        await userHelper.deleteUser(user.email, 404);
    });

    it("User - Create, update, retrieve and delete", async function (): Promise<void> {
        // CREATE lookups
        const title1 = await dbHelper.createTestLookup("Titles", "title");
        const role1 = await dbHelper.createTestLookup("PersonnelRoles", "personnel role");
        const department1 = await dbHelper.createTestLookup("Departments", "department");

        // CREATE user
        const email = LookupHelper.createTestEmail();
        const createResponse = await userHelper.createUser(await userHelper.createUserRequestBody({ email, title: title1, role: role1, department: department1 }));
        expect(createResponse.data).is.not.undefined;
        const user = createResponse.data;
        expect(user.email).equals(email);
        expect(user.departmentName).equals(department1.name);
        expect(user.roleName).equals(role1.name);
        expect(user.titleName).equals(title1.name);

        // CREATE new lookups
        const title2 = await dbHelper.createTestLookup("Titles", "title");
        const role2 = await dbHelper.createTestLookup("PersonnelRoles", "personnel role");
        const department2 = await dbHelper.createTestLookup("Departments", "department");

        // UPDATE user
        user.email = user.email.toUpperCase();
        user.displayName = user.displayName + " UPDATED";
        user.departmentId = department2.id;
        user.roleId = role2.id;
        user.titleId = title2.id;
        const updateResponse = await userHelper.updateUser(email, user);
        expect(updateResponse.data).is.not.undefined;
        const updatedUser = updateResponse.data;
        expect(updatedUser.email).equals(user.email.toLowerCase());
        expect(updatedUser.displayName).equals(user.displayName);
        expect(updatedUser.departmentName).equals(department2.name);
        expect(updatedUser.roleName).equals(role2.name);
        expect(updatedUser.titleName).equals(title2.name);

        // GET user
        const retrieveResponse = await userHelper.getUser(email);
        expect(retrieveResponse.data).is.not.undefined;
        const retrievedUser = retrieveResponse.data!;
        expect(retrievedUser.email).equals(user.email.toLowerCase());
        expect(retrievedUser.displayName).equals(user.displayName);
        expect(retrievedUser.departmentName).equals(department2.name);
        expect(retrievedUser.roleName).equals(role2.name);
        expect(retrievedUser.titleName).equals(title2.name);

        // DELETE user
        await userHelper.deleteUser(email)
    });

    describe("User Exceptions", async function (): Promise<void> {
        it("User - Get - 404 unknown email", async function (): Promise<void> {
            await userHelper.getUser("UNKNOWN_EMAIL", 404);
        });

        it("User - Create - 400 missing parameters", async function (): Promise<void> {
            await userHelper.createUser(<any>{ email: LookupHelper.createTestEmail() }, 400);
        });

        it("User - Create - 409 user already exists", async function (): Promise<void> {
            const existingUser = await dbHelper.createTestUser();
            const req = await userHelper.createUserRequestBody({ email: existingUser.email });
            await userHelper.createUser(req, 409);
        });

        it("User - Create - 404 unknown department", async function (): Promise<void> {
            const req = await userHelper.createUserRequestBody({ department: { id: "UNKNOWN_DEPARTMENT_ID", name: "UNKNOWN_DEPARTMENT", status: "Active" } });
            await userHelper.createUser(req, 404);
        });

        it("User - Create - 404 unknown personnel role", async function (): Promise<void> {
            const req = await userHelper.createUserRequestBody({ role: { id: "UNKNOWN_PERSONNEL_ROLE_ID", name: "UNKNOWN_PERSONNEL_ROLE", status: "Active" } });
            await userHelper.createUser(req, 404);
        });

        it("User - Create - 404 unknown title", async function (): Promise<void> {
            const req = await userHelper.createUserRequestBody({ title: { id: "UNKNOWN_TITLE_ID", name: "UNKNOWN_TITLE", status: "Active" } });
            await userHelper.createUser(req, 404);
        });

        it("User - Create - 400 input strings too long", async function (): Promise<void> {
            const longString = "this is a very long string which is greater than 64 characters in length";
            const req = await userHelper.createUserRequestBody();
            req.displayName = `${longString} -- ${longString} -- ${longString} -- ${longString}`;
            await userHelper.createUser(req, 400);
            req.displayName = "__test_Test User";
            req.firstName = longString;
            await userHelper.createUser(req, 400);
            req.firstName = "__test_Test";
            req.lastName = longString;
            await userHelper.createUser(req, 400);
            req.lastName = "__test_User";
            req.email = `${longString}${longString}${longString}${longString}${longString}@contoso.com`.replace(/ /g, '');
            await userHelper.createUser(req, 400);
        });

        it("User - Update - 400 mismatched path parameter", async function (): Promise<void> {
            const user: IUserCreateUpdateParams = {
                accessLevel: "tier3",
                departmentId: "dept1",
                displayName: "Unknown User",
                email: "UNKNOWN_EMAIL_2",
                firstName: "Unknown",
                lastName: "User",
                roleId: "role1"
            };
            await userHelper.updateUser("UNKNOWN_EMAIL_1", user, 400);
        });

        it("User - Update - 404 unknown email", async function (): Promise<void> {
            const user: IUserCreateUpdateParams = {
                accessLevel: "tier3",
                departmentId: "dept1",
                displayName: "Unknown User",
                email: "UNKNOWN_EMAIL",
                firstName: "Unknown",
                lastName: "User",
                roleId: "role1"
            };
            await userHelper.updateUser(user.email, user, 404);
        });

        it("User - Update - 404 unknown department", async function (): Promise<void> {
            const user = await dbHelper.createTestUser();
            user.departmentId = "UNKNOWN_DEPARTMENT_ID";
            await userHelper.updateUser(user.email, user, 404);
        });

        it("User - Update - 404 unknown personnel role", async function (): Promise<void> {
            const user = await dbHelper.createTestUser();
            user.roleId = "UNKNOWN_ROLE_ID";
            await userHelper.updateUser(user.email, user, 404);
        });

        it("User - Update - 404 unknown title", async function (): Promise<void> {
            const user = await dbHelper.createTestUser();
            user.titleId = "UNKNOWN_TITLE_ID";
            await userHelper.updateUser(user.email, user, 404);
        });

        it("User - Update - 400 input strings too long", async function (): Promise<void> {
            const longString = "this is a very long string which is greater than 64 characters in length";
            const createResponse = await userHelper.createUser(await userHelper.createUserRequestBody());
            const newUser = createResponse.data;
            newUser.displayName = `${longString} -- ${longString} -- ${longString} -- ${longString}`;
            await userHelper.updateUser(newUser.email, newUser, 400);
            newUser.displayName = "__test_Test User";
            newUser.firstName = longString;
            await userHelper.updateUser(newUser.email, newUser, 400);
            newUser.firstName = "__test_Test";
            newUser.lastName = longString;
            await userHelper.updateUser(newUser.email, newUser, 400);
            newUser.lastName = "__test_User";
            newUser.email = `${longString}${longString}${longString}${longString}${longString}@contoso.com`.replace(/ /g, '');
            await userHelper.updateUser(newUser.email, newUser, 400);
        });

        it("User - Delete - 404 unknown email", async function (): Promise<void> {
            await userHelper.deleteUser("UNKNOWN_EMAIL", 404);
        });
    });
});