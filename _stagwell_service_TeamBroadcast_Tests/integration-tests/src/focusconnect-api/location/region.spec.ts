import chai, { expect } from "chai";
import chaiAsPromised from "chai-as-promised";
import { CountryHelper } from "./country-helper";
import { StateHelper } from "./state-helper";
import { RegionHelper } from "./region-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { SubRegionHelper } from "./subregion-helper";
import { CosmosDbHelper } from "../../helpers/cosmos-helper";

chai.use(chaiAsPromised);

const countryHelper = new CountryHelper();
const stateHelper = new StateHelper();
const regionHelper = new RegionHelper();
const subRegionHelper = new SubRegionHelper();
const dbHelper = new CosmosDbHelper();

describe("Region", async function (): Promise<void> {

    before(async function () {
        console.log("Region Before...");
    });

    after(async function () {
        console.log("Region After...");
        await dbHelper.deleteTestData(false, ["Locations"]);
    });

    this.timeout(360000);

    it("Region - List all regions across the hierarchy", async function (): Promise<void> {
        // CREATE first test country, state, region
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
        const createRegionRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test region 1",
            nameAbbreviated: "tr1",
            countryId: createCountryRequest1.id,
            stateId: createStateRequest1.id
        };
        await regionHelper.createRegion(createRegionRequest1.countryId, createRegionRequest1.stateId, createRegionRequest1);

        // CREATE second test country, state, region
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
        const createRegionRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test region 2",
            nameAbbreviated: "tr2",
            countryId: createCountryRequest2.id,
            stateId: createStateRequest2.id
        };
        await regionHelper.createRegion(createRegionRequest2.countryId, createRegionRequest2.stateId, createRegionRequest2);

        // GET regions
        const retrieveResponse = await regionHelper.getRegions();
        const idList = retrieveResponse.data.map(c => c.id);

        // List should contain both test regions
        expect(idList).to.contain(createRegionRequest1.id);
        expect(idList).to.contain(createRegionRequest2.id);
    });

    it("Region - Retrieve all regions for a country and state", async function (): Promise<void> {
        // CREATE test country, state and two regions
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
        const createRegionRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test region 1",
            nameAbbreviated: "tr1",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id
        };
        await regionHelper.createRegion(createRegionRequest1.countryId, createRegionRequest1.stateId, createRegionRequest1);
        const createRegionRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test region 2",
            nameAbbreviated: "tr2",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id
        };
        await regionHelper.createRegion(createRegionRequest2.countryId, createRegionRequest2.stateId, createRegionRequest2);

        // GET regions
        const retrieveResponse1 = await regionHelper.getRegionsByState(createCountryRequest.id, createStateRequest.id);
        const idList1 = retrieveResponse1.data.map(c => c.id);

        // List should contain both test regions
        expect(idList1).to.contain(createRegionRequest1.id);
        expect(idList1).to.contain(createRegionRequest2.id);

        // DELETE one of the regions
        await regionHelper.deleteRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest2.id);

        // GET regions again
        const retrieveResponse2 = await regionHelper.getRegionsByState(createCountryRequest.id, createStateRequest.id);
        const idList2 = retrieveResponse2.data.map(c => c.id);

        // List should contain only one of the test regions
        expect(idList1).to.contain(createRegionRequest1.id);
        expect(idList2).to.not.contain(createRegionRequest2.id);
    });

    it("Region - Create, Update, Retrieve and Delete", async function (): Promise<void> {
        // CREATE test country, state and region
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
        const createRegionRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test region 1",
            nameAbbreviated: "tr1",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id
        };
        const createRegionResponse = await regionHelper.createRegion(createRegionRequest1.countryId, createRegionRequest1.stateId, createRegionRequest1);

        // Create response should match request
        expect(createRegionResponse.data.id).equals(createRegionRequest1.id);
        expect(createRegionResponse.data.name).equals(createRegionRequest1.name);
        expect(createRegionResponse.data.nameAbbreviated).equals(createRegionRequest1.nameAbbreviated);
        expect(createRegionResponse.data.status).equals("Active");
        expect(createRegionResponse.data.countryId).equals(createCountryRequest.id);
        expect(createRegionResponse.data.countryName).equals(createCountryRequest.name);
        expect(createRegionResponse.data.stateId).equals(createStateRequest.id);
        expect(createRegionResponse.data.stateName).equals(createStateRequest.name);
        expect(createRegionResponse.data.type).equals("region");

        // UPDATE region
        createRegionRequest1.name = `${createRegionRequest1.name} - UPDATED`;
        const updateRegionResponse = await regionHelper.updateRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest1.id, createRegionRequest1);

        // Update response should match updated request
        expect(updateRegionResponse.data.id).equals(createRegionRequest1.id);
        expect(updateRegionResponse.data.name).equals(createRegionRequest1.name);
        expect(updateRegionResponse.data.nameAbbreviated).equals(createRegionRequest1.nameAbbreviated);
        expect(updateRegionResponse.data.status).equals("Active");
        expect(updateRegionResponse.data.countryId).equals(createCountryRequest.id);
        expect(updateRegionResponse.data.countryName).equals(createCountryRequest.name);
        expect(updateRegionResponse.data.stateId).equals(createStateRequest.id);
        expect(updateRegionResponse.data.stateName).equals(createStateRequest.name);
        expect(updateRegionResponse.data.type).equals("region");

        // RETRIEVE region
        const retrieveRegionResponse1 = await regionHelper.getRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest1.id);
        const retrievedRegion = retrieveRegionResponse1.data!;

        // Retrieve response should match request
        expect(retrievedRegion.id).equals(createRegionRequest1.id);
        expect(retrievedRegion.name).equals(createRegionRequest1.name);
        expect(retrievedRegion.nameAbbreviated).equals(createRegionRequest1.nameAbbreviated);
        expect(retrievedRegion.status).equals("Active");
        expect(retrievedRegion.countryId).equals(createCountryRequest.id);
        expect(retrievedRegion.countryName).equals(createCountryRequest.name);
        expect(retrievedRegion.stateId).equals(createStateRequest.id);
        expect(retrievedRegion.stateName).equals(createStateRequest.name);
        expect(retrievedRegion.type).equals("region");
        expect(retrievedRegion.activeRelations?.length).equals(1);
        expect(retrievedRegion.activeRelations![0].name).equals("SubRegion");
        expect(retrievedRegion.activeRelations![0].activeCount).equals(0);

        // DELETE region
        await regionHelper.deleteRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest1.id);

        // RETRIEVE region again
        const retrieveRegionResponse2 = await regionHelper.getRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest1.id);

        // Region should have status of Deleted
        expect(retrieveRegionResponse2.data!.status).equals("Deleted");

        // CREATE second test region
        const createRegionRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test region 2",
            nameAbbreviated: "tr2",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id
        };
        await regionHelper.createRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest2);

        // UPDATE country name
        createCountryRequest.name = `${createCountryRequest.name} UPDATED`;
        await countryHelper.updateCountry(createCountryRequest.id, createCountryRequest);

        // UPDATE state name
        createStateRequest.name = `${createStateRequest.name} UPDATED`;
        await stateHelper.updateState(createCountryRequest.id, createStateRequest.id, createStateRequest);

        // Regions should have updated country name and state name
        const retrieveRegionResponse3 = await regionHelper.getRegion(createRegionRequest2.countryId, createRegionRequest2.stateId, createRegionRequest2.id);
        expect(retrieveRegionResponse3.data!.countryName).equals(createCountryRequest.name);
        expect(retrieveRegionResponse3.data!.stateName).equals(createStateRequest.name);
        const retrieveRegionResponse4 = await regionHelper.getRegion(createRegionRequest2.countryId, createRegionRequest2.stateId, createRegionRequest2.id);
        expect(retrieveRegionResponse4.data!.countryName).equals(createCountryRequest.name);
        expect(retrieveRegionResponse4.data!.stateName).equals(createStateRequest.name);
    });

    it("Region - Retrieve with relation", async function (): Promise<void> {
        // CREATE building
        const building = await dbHelper.createTestBuilding();

        // RETRIEVE region
        const retrieveRegionResponse = await regionHelper.getRegion(building.countryId, building.stateId, building.regionId);
        expect(retrieveRegionResponse.data).to.not.be.undefined;

        const retrievedRegion = retrieveRegionResponse.data!;
        expect(retrievedRegion.activeRelations).is.not.undefined;
        expect(retrievedRegion.activeRelations!.length).equals(1);
        expect(retrievedRegion.activeRelations![0].name).equals("SubRegion");
        expect(retrievedRegion.activeRelations![0].activeCount).equals(1);
    });

    it("Region - Update name and check cascade update", async function (): Promise<void> {
        // CREATE facility
        const dbHelper = new CosmosDbHelper();
        const facility = await dbHelper.createTestFacility();

        // RETRIEVE region
        const retrieveResponse = await regionHelper.getRegion(facility.countryId, facility.stateId, facility.regionId);
        const retrievedRegion = retrieveResponse.data!;

        // UPDATE region
        const updatedRegionName = `${retrievedRegion.name} - UPDATED`;
        retrievedRegion.name = updatedRegionName;
        const updateResponse = await regionHelper.updateRegion(retrievedRegion.countryId, retrievedRegion.stateId, retrievedRegion.id, retrievedRegion);
        expect(updateResponse.data.name).equals(updatedRegionName);

        // RETRIEVE region
        const updatedFacility = await dbHelper.getFacility(facility.id);
        expect(updatedFacility!.regionName).equals(updatedRegionName);
    });


    describe("Region Exceptions", async function (): Promise<void> {
        it("Region - Create - 409 Region Exists", async function (): Promise<void> {
            // CREATE test country, state and region
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
            const createRegionRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test region",
                nameAbbreviated: "tr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id
            };
            await regionHelper.createRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest);

            // CREATE region again with same input (should conflict)
            await regionHelper.createRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest, 409);
        });

        it("Region - Create - 400 Bad Request", async function (): Promise<void> {
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

            // CREATE test region with malformed input object
            const createRegionRequest = {
                id_INVALID: LookupHelper.createTestGuid(),
                name: "test region",
                nameAbbreviated: "tr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id
            };
            await regionHelper.createRegion(createCountryRequest.id, createStateRequest.id, <any>createRegionRequest, 400);
        });

        it("Region - Update - 404 Region Not Found", async function (): Promise<void> {
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

            // UPDATE region which doesn't exist
            const updateRegionRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test region",
                nameAbbreviated: "tr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id
            };
            await regionHelper.updateRegion(createCountryRequest.id, createStateRequest.id, updateRegionRequest.id, updateRegionRequest, 404);
        });

        it("Region - Update - 400 Bad Request", async function (): Promise<void> {
            // CREATE test country, state and region
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
            const createRegionRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test region",
                nameAbbreviated: "tr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id
            };
            await regionHelper.createRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest);

            // UPDATE region with malformed input
            const updateRegionRequest = {
                id: createRegionRequest.id,
                name_INVALID: "test region",
                nameAbbreviated: "tr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id
            };
            await regionHelper.updateRegion(createCountryRequest.id, createStateRequest.id, updateRegionRequest.id, <any>updateRegionRequest, 400);
        });

        it("Region - Update - 400 Path Parameter Mismatch", async function (): Promise<void> {
            // CREATE test country, state and region
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
            const createRegionRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test region",
                nameAbbreviated: "tr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id
            };
            await regionHelper.createRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest);

            // UPDATE region with mismatched path parameter
            const updateRegionRequest = {
                id: createRegionRequest.id,
                name: "test region UPDATED",
                nameAbbreviated: "tr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id
            };
            await regionHelper.updateRegion(createCountryRequest.id, createStateRequest.id, "INVALID_REGION_ID", updateRegionRequest, 400);
        });

        it("Region - Delete - 404 Region Not Found", async function (): Promise<void> {
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

            // DELETE region which doesn't exist
            await regionHelper.deleteRegion(createCountryRequest.id, createStateRequest.id, "UNKNOWN_REGION_ID", 404);
        });

        it("Region - Delete - 409 active children exist", async function (): Promise<void> {
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
            const createSubRegionRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test subregion",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id
            }
            await subRegionHelper.createSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest);

            // DELETE
            await regionHelper.deleteRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, 409);
        });
    });
});