import chai, { expect } from "chai";
import chaiAsPromised from "chai-as-promised";
import { CountryHelper } from "./country-helper";
import { StateHelper } from "./state-helper";
import { RegionHelper } from "./region-helper";
import { SubRegionHelper } from "./subregion-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { BuildingHelper } from "./building-helper";
import { CosmosDbHelper } from "../../helpers/cosmos-helper";

chai.use(chaiAsPromised);

const countryHelper = new CountryHelper();
const stateHelper = new StateHelper();
const regionHelper = new RegionHelper();
const subRegionHelper = new SubRegionHelper();
const buildingHelper = new BuildingHelper();
const dbHelper = new CosmosDbHelper();

describe("Subregion", async function (): Promise<void> {

    before(async function () {
        console.log("Subregion Before...");
    });

    after(async function () {
        console.log("Subregion After...");
        await dbHelper.deleteTestData(false, ["Locations"]);
    });

    this.timeout(360000);

    it("SubRegion - List all subregions across the hierarchy", async function (): Promise<void> {
        // CREATE first test country, state, region, subregion
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
        const createSubRegionRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test subregion 1",
            nameAbbreviated: "tsr1",
            countryId: createCountryRequest1.id,
            stateId: createStateRequest1.id,
            regionId: createRegionRequest1.id
        };
        await subRegionHelper.createSubRegion(createSubRegionRequest1.countryId, createSubRegionRequest1.stateId, createSubRegionRequest1.regionId, createSubRegionRequest1);

        // CREATE second test country, state, region, subregion
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
        const createSubRegionRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test subregion 2",
            nameAbbreviated: "tsr2",
            countryId: createCountryRequest2.id,
            stateId: createStateRequest2.id,
            regionId: createRegionRequest2.id
        };
        await subRegionHelper.createSubRegion(createSubRegionRequest2.countryId, createSubRegionRequest2.stateId, createSubRegionRequest2.regionId, createSubRegionRequest2);

        // GET subregions
        const retrieveResponse = await subRegionHelper.getSubRegions();
        const idList = retrieveResponse.data.map(c => c.id);

        // List should contain both test subregions
        expect(idList).to.contain(createSubRegionRequest1.id);
        expect(idList).to.contain(createSubRegionRequest2.id);
    });

    it("Subregion - Retrieve all subregions for a country, state and region", async function (): Promise<void> {
        // CREATE test country, state, region and two subregions
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
        await regionHelper.createRegion(createRegionRequest.countryId, createRegionRequest.stateId, createRegionRequest);
        const createSubRegionRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test subregion 1",
            nameAbbreviated: "tsr1",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id,
            regionId: createRegionRequest.id
        };
        await subRegionHelper.createSubRegion(createSubRegionRequest1.countryId, createSubRegionRequest1.stateId, createSubRegionRequest1.regionId, createSubRegionRequest1);
        const createSubRegionRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test subregion 2",
            nameAbbreviated: "tsr2",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id,
            regionId: createRegionRequest.id
        };
        await subRegionHelper.createSubRegion(createSubRegionRequest2.countryId, createSubRegionRequest2.stateId, createSubRegionRequest2.regionId, createSubRegionRequest2);

        // GET subregions
        const retrieveResponse1 = await subRegionHelper.getSubRegionsByRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id);
        const idList1 = retrieveResponse1.data.map(c => c.id);

        // List should contain both test subregions
        expect(idList1).to.contain(createSubRegionRequest1.id);
        expect(idList1).to.contain(createSubRegionRequest2.id);

        // DELETE one of the subregions
        await subRegionHelper.deleteSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest2.id);

        // GET subregions again
        const retrieveResponse2 = await subRegionHelper.getSubRegionsByRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id);
        const idList2 = retrieveResponse2.data.map(c => c.id);

        // List should contain only one of the test regions
        expect(idList1).to.contain(createSubRegionRequest1.id);
        expect(idList2).to.not.contain(createSubRegionRequest2.id);
    });

    it("Subregion - Create, Update, Retrieve and Delete", async function (): Promise<void> {
        // CREATE test country, state, region and subregion
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
        const createRegionResponse = await regionHelper.createRegion(createRegionRequest.countryId, createRegionRequest.stateId, createRegionRequest);
        const createSubRegionRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test subregion 1",
            nameAbbreviated: "tsr1",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id,
            regionId: createRegionRequest.id
        };
        const createSubRegionResponse1 = await subRegionHelper.createSubRegion(createSubRegionRequest1.countryId, createSubRegionRequest1.stateId, createSubRegionRequest1.regionId, createSubRegionRequest1);

        // Create response should match request
        expect(createSubRegionResponse1.data.id).equals(createSubRegionRequest1.id);
        expect(createSubRegionResponse1.data.name).equals(createSubRegionRequest1.name);
        expect(createSubRegionResponse1.data.nameAbbreviated).equals(createSubRegionRequest1.nameAbbreviated);
        expect(createSubRegionResponse1.data.status).equals("Active");
        expect(createSubRegionResponse1.data.countryId).equals(createCountryRequest.id);
        expect(createSubRegionResponse1.data.countryName).equals(createCountryRequest.name);
        expect(createSubRegionResponse1.data.stateId).equals(createStateRequest.id);
        expect(createSubRegionResponse1.data.stateName).equals(createStateRequest.name);
        expect(createSubRegionResponse1.data.regionId).equals(createRegionRequest.id);
        expect(createSubRegionResponse1.data.regionName).equals(createRegionRequest.name);
        expect(createSubRegionResponse1.data.type).equals("subregion");

        // UPDATE subregion
        createSubRegionRequest1.name = `${createSubRegionRequest1.name} - UPDATED`;
        const updateSubRegionResponse = await subRegionHelper.updateSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest1.id, createSubRegionRequest1);

        // Update response should match updated request
        expect(updateSubRegionResponse.data.id).equals(createSubRegionRequest1.id);
        expect(updateSubRegionResponse.data.name).equals(createSubRegionRequest1.name);
        expect(updateSubRegionResponse.data.nameAbbreviated).equals(createSubRegionRequest1.nameAbbreviated);
        expect(updateSubRegionResponse.data.status).equals("Active");
        expect(updateSubRegionResponse.data.countryId).equals(createCountryRequest.id);
        expect(updateSubRegionResponse.data.countryName).equals(createCountryRequest.name);
        expect(updateSubRegionResponse.data.stateId).equals(createStateRequest.id);
        expect(updateSubRegionResponse.data.stateName).equals(createStateRequest.name);
        expect(updateSubRegionResponse.data.regionId).equals(createRegionRequest.id);
        expect(updateSubRegionResponse.data.regionName).equals(createRegionRequest.name);
        expect(updateSubRegionResponse.data.type).equals("subregion");

        // RETRIEVE subregion
        const retrieveSubRegionResponse1 = await subRegionHelper.getSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest1.id);
        const retrievedSubRegion1 = retrieveSubRegionResponse1.data!;

        // Retrieve response should match request
        expect(retrievedSubRegion1.id).equals(createSubRegionRequest1.id);
        expect(retrievedSubRegion1.name).equals(createSubRegionRequest1.name);
        expect(retrievedSubRegion1.nameAbbreviated).equals(createSubRegionRequest1.nameAbbreviated);
        expect(retrievedSubRegion1.status).equals("Active");
        expect(retrievedSubRegion1.countryId).equals(createCountryRequest.id);
        expect(retrievedSubRegion1.countryName).equals(createCountryRequest.name);
        expect(retrievedSubRegion1.stateId).equals(createStateRequest.id);
        expect(retrievedSubRegion1.stateName).equals(createStateRequest.name);
        expect(retrievedSubRegion1.regionId).equals(createRegionRequest.id);
        expect(retrievedSubRegion1.regionName).equals(createRegionRequest.name);
        expect(retrievedSubRegion1.type).equals("subregion");
        expect(retrievedSubRegion1.activeRelations?.length).equals(1);
        expect(retrievedSubRegion1.activeRelations![0].name).equals("Building");
        expect(retrievedSubRegion1.activeRelations![0].activeCount).equals(0);

        // DELETE subregion
        await subRegionHelper.deleteSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest1.id);

        // RETRIEVE subregion again
        const retrieveSubRegionResponse2 = await subRegionHelper.getSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest1.id);

        // Subregion should have status of Deleted
        expect(retrieveSubRegionResponse2.data!.status).equals("Deleted");

        // CREATE second test subregion
        const createSubRegionRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test subregion 2",
            nameAbbreviated: "tsr2",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id,
            regionId: createRegionRequest.id
        };
        await subRegionHelper.createSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest2);

        // UPDATE country name
        createCountryRequest.name = `${createCountryRequest.name} UPDATED`;
        await countryHelper.updateCountry(createCountryRequest.id, createCountryRequest);

        // UPDATE state name
        createStateRequest.name = `${createStateRequest.name} UPDATED`;
        await stateHelper.updateState(createCountryRequest.id, createStateRequest.id, createStateRequest);

        // UPDATE region name
        createRegionRequest.name = `${createRegionRequest.name} UPDATED`;
        await regionHelper.updateRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createRegionRequest);

        // Subregions should have updated country name and state name and region name
        const retrieveSubRegionResponse3 = await subRegionHelper.getSubRegion(createSubRegionRequest2.countryId, createSubRegionRequest2.stateId, createSubRegionRequest2.regionId, createSubRegionRequest2.id);
        expect(retrieveSubRegionResponse3.data!.countryName).equals(createCountryRequest.name);
        expect(retrieveSubRegionResponse3.data!.stateName).equals(createStateRequest.name);
        expect(retrieveSubRegionResponse3.data!.regionName).equals(createRegionRequest.name);
        const retrieveSubRegionResponse4 = await subRegionHelper.getSubRegion(createSubRegionRequest2.countryId, createSubRegionRequest2.stateId, createSubRegionRequest2.regionId, createSubRegionRequest2.id);
        expect(retrieveSubRegionResponse4.data!.countryName).equals(createCountryRequest.name);
        expect(retrieveSubRegionResponse4.data!.stateName).equals(createStateRequest.name);
        expect(retrieveSubRegionResponse4.data!.regionName).equals(createRegionRequest.name);
    });

    it("Subregion - Retrieve with relation", async function (): Promise<void> {
        // CREATE building
        const building = await dbHelper.createTestBuilding();

        // RETRIEVE subregion
        const retrieveSubregionResponse = await subRegionHelper.getSubRegion(building.countryId, building.stateId, building.regionId, building.subRegionId);
        expect(retrieveSubregionResponse.data).to.not.be.undefined;

        const retrievedSubregion = retrieveSubregionResponse.data!;
        expect(retrievedSubregion.activeRelations).is.not.undefined;
        expect(retrievedSubregion.activeRelations!.length).equals(1);
        expect(retrievedSubregion.activeRelations![0].name).equals("Building");
        expect(retrievedSubregion.activeRelations![0].activeCount).equals(1);
    });

    it("Subregion - Update name and check cascade update", async function (): Promise<void> {
        // CREATE facility
        const dbHelper = new CosmosDbHelper();
        const facility = await dbHelper.createTestFacility();

        // RETRIEVE subregion
        const retrieveResponse = await subRegionHelper.getSubRegion(facility.countryId, facility.stateId, facility.regionId, facility.subRegionId);
        const retrievedSubregion = retrieveResponse.data!;

        // UPDATE subregion
        const updatedSubregionName = `${retrievedSubregion.name} - UPDATED`;
        retrievedSubregion.name = updatedSubregionName;
        const updateResponse = await subRegionHelper.updateSubRegion(retrievedSubregion.countryId, retrievedSubregion.stateId, retrievedSubregion.regionId, retrievedSubregion.id, retrievedSubregion);
        expect(updateResponse.data.name).equals(updatedSubregionName);

        // RETRIEVE facility
        const updatedFacility = await dbHelper.getFacility(facility.id);
        expect(updatedFacility!.subRegionName).equals(updatedSubregionName);
    });


    describe("Subregion Exceptions", async function (): Promise<void> {
        it("Subregion - Create - 409 Subregion Exists", async function (): Promise<void> {
            // CREATE test country, state, region and subregion
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
            const createSubRegionRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test subregion",
                nameAbbreviated: "tsr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id
            };
            await subRegionHelper.createSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest);

            // CREATE subregion again with same input (should conflict)
            await subRegionHelper.createSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest, 409);
        });

        it("Subregion - Create - 400 Bad Request", async function (): Promise<void> {
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

            // CREATE test subregion with malformed input object
            const createSubRegionRequest = {
                id_INVALID: LookupHelper.createTestGuid(),
                name: "test subregion",
                nameAbbreviated: "tsr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id
            };
            await subRegionHelper.createSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, <any>createSubRegionRequest, 400);
        });

        it("Subregion - Update - 404 Subregion Not Found", async function (): Promise<void> {
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

            // UPDATE subregion which doesn't exist
            const updateSubRegionRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test subregion",
                nameAbbreviated: "tsr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id
            };
            await subRegionHelper.updateSubRegion(createCountryRequest.id, createStateRequest.id, updateSubRegionRequest.regionId, updateSubRegionRequest.id, updateSubRegionRequest, 404);
        });

        it("Subregion - Update - 400 Bad Request", async function (): Promise<void> {
            // CREATE test country, state, region and subregion
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
            const createSubRegionRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test subregion",
                nameAbbreviated: "tsr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id
            };
            await subRegionHelper.createSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest);

            // UPDATE subregion with malformed input
            const updateSubRegionRequest = {
                id: createSubRegionRequest.id,
                name_INVALID: "test subregion",
                nameAbbreviated: "tsr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id
            };
            await subRegionHelper.updateSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, <any>updateSubRegionRequest, 400);
        });

        it("Subregion - Update - 400 Path Parameter Mismatch", async function (): Promise<void> {
            // CREATE test country, state, region and subregion
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
            const createSubRegionRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test subregion",
                nameAbbreviated: "tsr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id
            };
            await subRegionHelper.createSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest);

            // UPDATE subregion with mismatched path parameter
            const updateSubRegionRequest = {
                id: createSubRegionRequest.id,
                name: "test subregion UPDATED",
                nameAbbreviated: "tsr",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id
            };
            await subRegionHelper.updateSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, "INVALID_SUBREGION_ID", updateSubRegionRequest, 400);
        });

        it("Subregion - Delete - 404 Subregion Not Found", async function (): Promise<void> {
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

            // DELETE subregion which doesn't exist
            await subRegionHelper.deleteSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, "UNKNOWN_SUBREGION_ID", 404);
        });

        it("Subregion - Delete - 409 active children exist", async function (): Promise<void> {
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
            const createBuildingRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test building",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id,
                subRegionId: createSubRegionRequest.id
            }
            await buildingHelper.createBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest);

            // DELETE
            await subRegionHelper.deleteSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, 409);
        });
    });
});