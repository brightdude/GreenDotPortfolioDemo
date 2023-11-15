import chai, { expect } from "chai";
import chaiAsPromised from "chai-as-promised";
import { CountryHelper } from "./country-helper";
import { StateHelper } from "./state-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { RegionHelper } from "./region-helper";
import { CosmosDbHelper } from "../../helpers/cosmos-helper";

chai.use(chaiAsPromised);

const countryHelper = new CountryHelper();
const stateHelper = new StateHelper();
const regionHelper = new RegionHelper();
const dbHelper = new CosmosDbHelper();

describe("State", async function (): Promise<void> {

    before(async function () {
        console.log("State Before...");
    });

    after(async function () {
        console.log("State After...");
        try {
            console.log("Start teardown for State...");
            await dbHelper.deleteTestData(false, ["Locations"]);
            console.log("End teardown for State.");
        }
        catch (ex) {
            console.error(ex);
        }
    });

    this.timeout(360000);

    it("State - List all states across the hierarchy", async function (): Promise<void> {
        // CREATE first test country, state
        const createCountryRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test country 1"
        };
        await countryHelper.createCountry(createCountryRequest1);
        const createStateRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test state 1",
            countryId: createCountryRequest1.id
        };
        await stateHelper.createState(createCountryRequest1.id, createStateRequest1);

        // CREATE second test country, state
        const createCountryRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test country 2"
        };
        await countryHelper.createCountry(createCountryRequest2);
        const createStateRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test state 2",
            countryId: createCountryRequest2.id
        };
        await stateHelper.createState(createCountryRequest2.id, createStateRequest2);

        // GET states
        const retrieveResponse = await stateHelper.getStates();
        const idList = retrieveResponse.data.map(c => c.id);

        // List should contain both test states
        expect(idList).to.contain(createStateRequest1.id);
        expect(idList).to.contain(createStateRequest2.id);
    });

    it("State - Retrieve all states for a country", async function (): Promise<void> {
        // CREATE test country and two states
        const createCountryRequest = {
            id: LookupHelper.createTestGuid(),
            name: "test country"
        };
        await countryHelper.createCountry(createCountryRequest);
        const createStateRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test state 1",
            countryId: createCountryRequest.id
        };
        await stateHelper.createState(createCountryRequest.id, createStateRequest1);
        const createStateRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test state 2",
            countryId: createCountryRequest.id
        };
        await stateHelper.createState(createCountryRequest.id, createStateRequest2);

        // GET states
        const retrieveResponse1 = await stateHelper.getStatesByCountry(createCountryRequest.id);
        const idList1 = retrieveResponse1.data.map(c => c.id);

        // List should contain both test states
        expect(idList1).to.contain(createStateRequest1.id);
        expect(idList1).to.contain(createStateRequest2.id);

        // DELETE one of the states
        await stateHelper.deleteState(createCountryRequest.id, createStateRequest2.id);

        // GET states again
        const retrieveResponse2 = await stateHelper.getStatesByCountry(createCountryRequest.id);
        const idList2 = retrieveResponse2.data.map(c => c.id);

        // List should contain only one of the test states
        expect(idList1).to.contain(createStateRequest1.id);
        expect(idList2).to.not.contain(createStateRequest2.id);
    });

    it("State - Create, Update, Retrieve and Delete", async function (): Promise<void> {
        // CREATE test country and state
        const createCountryRequest = {
            id: LookupHelper.createTestGuid(),
            name: "test country"
        };
        await countryHelper.createCountry(createCountryRequest);
        const createStateRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test state 1",
            countryId: createCountryRequest.id
        };
        const createStateResponse1 = await stateHelper.createState(createCountryRequest.id, createStateRequest1);

        // Create response should match request
        expect(createStateResponse1.data.id).equals(createStateRequest1.id);
        expect(createStateResponse1.data.name).equals(createStateRequest1.name);
        expect(createStateResponse1.data.status).equals("Active");
        expect(createStateResponse1.data.countryId).equals(createCountryRequest.id);
        expect(createStateResponse1.data.countryName).equals(createCountryRequest.name);
        expect(createStateResponse1.data.type).equals("state");

        // UPDATE state
        createStateRequest1.name = createStateRequest1.name + " - UPDATED";
        const updateStateResponse = await stateHelper.updateState(createCountryRequest.id, createStateRequest1.id, createStateRequest1);

        // Update response should match updated request
        expect(updateStateResponse.data.id).equals(createStateRequest1.id);
        expect(updateStateResponse.data.name).equals(createStateRequest1.name);
        expect(updateStateResponse.data.status).equals("Active");
        expect(updateStateResponse.data.countryId).equals(createCountryRequest.id);
        expect(updateStateResponse.data.countryName).equals(createCountryRequest.name);
        expect(updateStateResponse.data.type).equals("state");

        // RETRIEVE state
        const retrieveStateResponse1 = await stateHelper.getState(createCountryRequest.id, createStateRequest1.id);
        const retrievedState1 = retrieveStateResponse1.data!;

        // Retrieve response should match request
        expect(retrievedState1.id).equals(createStateRequest1.id);
        expect(retrievedState1.name).equals(createStateRequest1.name);
        expect(retrievedState1.status).equals("Active");
        expect(retrievedState1.countryId).equals(createCountryRequest.id);
        expect(retrievedState1.countryName).equals(createCountryRequest.name);
        expect(retrievedState1.type).equals("state");
        expect(retrievedState1.activeRelations?.length).equals(1);
        expect(retrievedState1.activeRelations![0].name).equals("Region");
        expect(retrievedState1.activeRelations![0].activeCount).equals(0);

        // DELETE state
        await stateHelper.deleteState(createCountryRequest.id, createStateRequest1.id);

        // RETRIEVE state again
        const retrieveStateResponse2 = await stateHelper.getState(createCountryRequest.id, createStateRequest1.id);

        // State should have status of Deleted
        expect(retrieveStateResponse2.data!.status).equals("Deleted");

        // CREATE second test state
        const createStateRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test state 2",
            countryId: createCountryRequest.id
        };
        await stateHelper.createState(createCountryRequest.id, createStateRequest2);

        // UPDATE country name
        createCountryRequest.name = `${createCountryRequest.name} UPDATED`;
        await countryHelper.updateCountry(createCountryRequest.id, createCountryRequest);

        // States should have updated country name
        const retrieveStateResponse3 = await stateHelper.getState(createStateRequest2.countryId, createStateRequest2.id);
        expect(retrieveStateResponse3.data!.countryName).equals(createCountryRequest.name);
        const retrieveStateResponse4 = await stateHelper.getState(createStateRequest2.countryId, createStateRequest2.id);
        expect(retrieveStateResponse4.data!.countryName).equals(createCountryRequest.name);
    });

    it("State - Retrieve with relation", async function (): Promise<void> {
        // CREATE building
        const building = await dbHelper.createTestBuilding();

        // RETRIEVE state
        const retrieveStateResponse = await stateHelper.getState(building.countryId, building.stateId);
        expect(retrieveStateResponse.data).to.not.be.undefined;

        const retrievedState = retrieveStateResponse.data!;
        expect(retrievedState.activeRelations).is.not.undefined;
        expect(retrievedState.activeRelations!.length).equals(1);
        expect(retrievedState.activeRelations![0].name).equals("Region");
        expect(retrievedState.activeRelations![0].activeCount).equals(1);
    });

    it("State - Update name and check cascade update", async function (): Promise<void> {
        // CREATE facility
        const dbHelper = new CosmosDbHelper();
        const facility = await dbHelper.createTestFacility();

        // RETRIEVE state
        const retrieveResponse = await stateHelper.getState(facility.countryId, facility.stateId);
        const retrievedState = retrieveResponse.data!;

        // UPDATE state
        const updatedStateName = `${retrievedState.name} - UPDATED`;
        retrievedState.name = updatedStateName;
        const updateResponse = await stateHelper.updateState(retrievedState.countryId, retrievedState.id, retrievedState);
        expect(updateResponse.data.name).equals(updatedStateName);

        // RETRIEVE facility
        const updatedFacility = await dbHelper.getFacility(facility.id);
        expect(updatedFacility!.stateName).equals(updatedStateName);
    });

    describe("State Exceptions", async function (): Promise<void> {
        it("State - Create - 409 State Exists", async function (): Promise<void> {
            // CREATE test country and state
            const createCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(createCountryRequest);
            const createStateRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test state",
                countryId: createCountryRequest.id
            };
            await stateHelper.createState(createCountryRequest.id, createStateRequest);

            // CREATE again with same input (should conflict)
            await stateHelper.createState(createCountryRequest.id, createStateRequest, 409);
        });

        it("State - Create - 400 Bad Request", async function (): Promise<void> {
            // CREATE test country
            const createCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(createCountryRequest);

            // CREATE test state with malformed input object
            const createStateRequest = {
                id_INVALID: LookupHelper.createTestGuid(),
                name: "test state",
                countryId: createCountryRequest.id
            };
            await stateHelper.createState(createCountryRequest.id, <any>createStateRequest, 400);
        });

        it("State - Update - 404 State Not Found", async function (): Promise<void> {
            // CREATE test country
            const createCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(createCountryRequest);

            // UPDATE state which doesn't exist
            const updateStateRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test state",
                countryId: createCountryRequest.id
            };
            await stateHelper.updateState(createCountryRequest.id, updateStateRequest.id, updateStateRequest, 404);
        });

        it("State - Update - 400 Bad Request", async function (): Promise<void> {
            // CREATE test country and state
            const createCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(createCountryRequest);
            const createStateRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test state",
                countryId: createCountryRequest.id
            };
            await stateHelper.createState(createCountryRequest.id, createStateRequest);

            // UPDATE state with malformed input
            const updateStateRequest = {
                id: createStateRequest.id,
                name_INVALID: "test state"
            };
            await stateHelper.updateState(createCountryRequest.id, createStateRequest.id, <any>updateStateRequest, 400);
        });

        it("State - Update - 400 Path Parameter Mismatch", async function (): Promise<void> {
            // CREATE test country and state
            const createCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(createCountryRequest);
            const createStateRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test state",
                countryId: createCountryRequest.id
            };
            await stateHelper.createState(createCountryRequest.id, createStateRequest);

            // UPDATE state with mismatched path parameter
            const updateStateRequest = {
                id: createStateRequest.id,
                name: "test state UPDATED",
                countryId: createCountryRequest.id
            };
            await stateHelper.updateState(createCountryRequest.id, "INVALID_STATE_ID", updateStateRequest, 400);
        });

        it("State - Delete - 404 State Not Found", async function (): Promise<void> {
            // CREATE test country
            const createCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(createCountryRequest);

            // DELETE state which doesn't exist
            await stateHelper.deleteState(createCountryRequest.id, "UNKNOWN_STATE_ID", 404);
        });

        it("State - Delete - 409 active children exist", async function (): Promise<void> {
            // CREATE
            const createCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(createCountryRequest);
            const createStateRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test state",
                countryId: createCountryRequest.id
            }
            await stateHelper.createState(createCountryRequest.id, createStateRequest);
            const createRegionRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test region",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id
            }
            await regionHelper.createRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest);

            // DELETE
            await stateHelper.deleteState(createCountryRequest.id, createStateRequest.id, 409);
        });
    });
});