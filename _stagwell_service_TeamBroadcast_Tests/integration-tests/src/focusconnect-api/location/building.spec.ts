import chai, { expect } from "chai";
import chaiAsPromised from "chai-as-promised";
import { CountryHelper } from "./country-helper";
import { StateHelper } from "./state-helper";
import { RegionHelper } from "./region-helper";
import { SubRegionHelper } from "./subregion-helper";
import { BuildingHelper } from "./building-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { CosmosDbHelper, ILocation } from "../../helpers/cosmos-helper";

chai.use(chaiAsPromised);

const countryHelper = new CountryHelper();
const stateHelper = new StateHelper();
const regionHelper = new RegionHelper();
const subRegionHelper = new SubRegionHelper();
const buildingHelper = new BuildingHelper();
const dbHelper = new CosmosDbHelper();

describe("Building", async function (): Promise<void> {

    before(async function () {
        console.log("Building Before...");
    });

    after(async function () {
        console.log("Building After...");
        await dbHelper.deleteTestData(false, ["Locations"]);
    });

    this.timeout(360000);

    it("Building - List all buildings across the hierarchy", async function (): Promise<void> {
        // CREATE first test country, state, region, subregion and building
        const createCountryRequest1: ILocation = {
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
        const createBuildingRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test building 1",
            nameAbbreviated: "tb1",
            countryId: createCountryRequest1.id,
            stateId: createStateRequest1.id,
            regionId: createRegionRequest1.id,
            subRegionId: createSubRegionRequest1.id
        };
        await buildingHelper.createBuilding(createBuildingRequest1.countryId, createBuildingRequest1.stateId, createBuildingRequest1.regionId, createBuildingRequest1.subRegionId, createBuildingRequest1);

        // CREATE second test country, state, region, subregion and building
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
        const createBuildingRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test building 2",
            nameAbbreviated: "tb2",
            countryId: createCountryRequest2.id,
            stateId: createStateRequest2.id,
            regionId: createRegionRequest2.id,
            subRegionId: createSubRegionRequest2.id
        };
        await buildingHelper.createBuilding(createBuildingRequest2.countryId, createBuildingRequest2.stateId, createBuildingRequest2.regionId, createBuildingRequest2.subRegionId, createBuildingRequest2);

        // GET buildings
        const retrieveResponse = await buildingHelper.getBuildings();
        const idList = retrieveResponse.data.map(c => c.id);

        // List should contain both test buildings
        expect(idList).to.contain(createBuildingRequest1.id);
        expect(idList).to.contain(createBuildingRequest2.id);
    });

    it("Building - Retrieve all buildings for a country, state and region", async function (): Promise<void> {
        // CREATE test country, state, region, subregion and two buildings
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
        const createSubRegionRequest = {
            id: LookupHelper.createTestGuid(),
            name: "test subregion",
            nameAbbreviated: "tsr",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id,
            regionId: createRegionRequest.id
        };
        await subRegionHelper.createSubRegion(createSubRegionRequest.countryId, createSubRegionRequest.stateId, createSubRegionRequest.regionId, createSubRegionRequest);
        const createBuildingRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test building 1",
            nameAbbreviated: "tb1",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id,
            regionId: createRegionRequest.id,
            subRegionId: createSubRegionRequest.id
        };
        await buildingHelper.createBuilding(createBuildingRequest1.countryId, createBuildingRequest1.stateId, createBuildingRequest1.regionId, createBuildingRequest1.subRegionId, createBuildingRequest1);
        const createBuildingRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test building 2",
            nameAbbreviated: "tb2",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id,
            regionId: createRegionRequest.id,
            subRegionId: createSubRegionRequest.id
        };
        await buildingHelper.createBuilding(createBuildingRequest2.countryId, createBuildingRequest2.stateId, createBuildingRequest2.regionId, createBuildingRequest2.subRegionId, createBuildingRequest2);

        // GET buildings
        const retrieveResponse1 = await buildingHelper.getBuildingsBySubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id);
        const idList1 = retrieveResponse1.data.map(c => c.id);

        // List should contain both test buildings
        expect(idList1).to.contain(createBuildingRequest1.id);
        expect(idList1).to.contain(createBuildingRequest2.id);

        // DELETE one of the buildings
        await buildingHelper.deleteBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest2.id);

        // GET buildings again
        const retrieveResponse2 = await buildingHelper.getBuildingsBySubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id);
        const idList2 = retrieveResponse2.data.map(c => c.id);

        // List should contain only one of the test regions
        expect(idList1).to.contain(createBuildingRequest1.id);
        expect(idList2).to.not.contain(createBuildingRequest2.id);
    });

    it("Building - Create, Update, Retrieve and Delete", async function (): Promise<void> {
        // CREATE test country, state, region, subregion and building
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
        const createSubRegionRequest = {
            id: LookupHelper.createTestGuid(),
            name: "test subregion",
            nameAbbreviated: "tsr",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id,
            regionId: createRegionRequest.id
        };
        const createSubRegionResponse = await subRegionHelper.createSubRegion(createSubRegionRequest.countryId, createSubRegionRequest.stateId, createSubRegionRequest.regionId, createSubRegionRequest);
        const createBuildingRequest1 = {
            id: LookupHelper.createTestGuid(),
            name: "test building 1",
            nameAbbreviated: "tb1",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id,
            regionId: createRegionRequest.id,
            subRegionId: createSubRegionRequest.id
        };
        const createBuildingResponse1 = await buildingHelper.createBuilding(createBuildingRequest1.countryId, createBuildingRequest1.stateId, createBuildingRequest1.regionId, createBuildingRequest1.subRegionId, createBuildingRequest1);

        // Create response should match request
        expect(createBuildingResponse1.data.id).equals(createBuildingRequest1.id);
        expect(createBuildingResponse1.data.name).equals(createBuildingRequest1.name);
        expect(createBuildingResponse1.data.nameAbbreviated).equals(createBuildingRequest1.nameAbbreviated);
        expect(createBuildingResponse1.data.status).equals("Active");
        expect(createBuildingResponse1.data.countryId).equals(createCountryRequest.id);
        expect(createBuildingResponse1.data.countryName).equals(createCountryRequest.name);
        expect(createBuildingResponse1.data.stateId).equals(createStateRequest.id);
        expect(createBuildingResponse1.data.stateName).equals(createStateRequest.name);
        expect(createBuildingResponse1.data.regionId).equals(createRegionRequest.id);
        expect(createBuildingResponse1.data.regionName).equals(createRegionRequest.name);
        expect(createBuildingResponse1.data.subRegionId).equals(createSubRegionRequest.id);
        expect(createBuildingResponse1.data.subRegionName).equals(createSubRegionRequest.name);
        expect(createBuildingResponse1.data.type).equals("building");

        // UPDATE building
        createBuildingRequest1.name = `${createBuildingRequest1.name} - UPDATED`;
        const updateBuildingResponse = await buildingHelper.updateBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest1.id, createBuildingRequest1);

        // Update response should match updated request
        expect(updateBuildingResponse.data.id).equals(createBuildingRequest1.id);
        expect(updateBuildingResponse.data.name).equals(createBuildingRequest1.name);
        expect(updateBuildingResponse.data.nameAbbreviated).equals(createBuildingRequest1.nameAbbreviated);
        expect(updateBuildingResponse.data.status).equals("Active");
        expect(updateBuildingResponse.data.countryId).equals(createCountryRequest.id);
        expect(updateBuildingResponse.data.countryName).equals(createCountryRequest.name);
        expect(updateBuildingResponse.data.stateId).equals(createStateRequest.id);
        expect(updateBuildingResponse.data.stateName).equals(createStateRequest.name);
        expect(updateBuildingResponse.data.regionId).equals(createRegionRequest.id);
        expect(updateBuildingResponse.data.regionName).equals(createRegionRequest.name);
        expect(updateBuildingResponse.data.subRegionId).equals(createSubRegionRequest.id);
        expect(updateBuildingResponse.data.subRegionName).equals(createSubRegionRequest.name);
        expect(updateBuildingResponse.data.type).equals("building");

        // RETRIEVE building
        const retrieveBuildingResponse1 = await buildingHelper.getBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest1.id);
        const retrievedBuilding1 = retrieveBuildingResponse1.data!;

        // Retrieve response should match request
        expect(retrievedBuilding1.id).equals(createBuildingRequest1.id);
        expect(retrievedBuilding1.name).equals(createBuildingRequest1.name);
        expect(retrievedBuilding1.nameAbbreviated).equals(createBuildingRequest1.nameAbbreviated);
        expect(retrievedBuilding1.status).equals("Active");
        expect(retrievedBuilding1.countryId).equals(createCountryRequest.id);
        expect(retrievedBuilding1.countryName).equals(createCountryRequest.name);
        expect(retrievedBuilding1.stateId).equals(createStateRequest.id);
        expect(retrievedBuilding1.stateName).equals(createStateRequest.name);
        expect(retrievedBuilding1.regionId).equals(createRegionRequest.id);
        expect(retrievedBuilding1.regionName).equals(createRegionRequest.name);
        expect(retrievedBuilding1.subRegionId).equals(createSubRegionRequest.id);
        expect(retrievedBuilding1.subRegionName).equals(createSubRegionRequest.name);
        expect(retrievedBuilding1.type).equals("building");
        expect(retrievedBuilding1.activeRelations?.length).equals(1);
        expect(retrievedBuilding1.activeRelations![0].name).equals("Facility");
        expect(retrievedBuilding1.activeRelations![0].activeCount).equals(0);

        // DELETE building
        await buildingHelper.deleteBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest1.id);

        // RETRIEVE building again
        const retrieveBuildingResponse2 = await buildingHelper.getBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest1.id);

        // Building should have status of Deleted
        expect(retrieveBuildingResponse2.data!.status).equals("Deleted");

        // CREATE second test building
        const createBuildingRequest2 = {
            id: LookupHelper.createTestGuid(),
            name: "test building 2",
            nameAbbreviated: "tb2",
            countryId: createCountryRequest.id,
            stateId: createStateRequest.id,
            regionId: createRegionRequest.id,
            subRegionId: createSubRegionRequest.id
        };
        await buildingHelper.createBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest2);

        // UPDATE country name
        createCountryRequest.name = `${createCountryRequest.name} UPDATED`;
        await countryHelper.updateCountry(createCountryRequest.id, createCountryRequest);

        // UPDATE state name
        createStateRequest.name = `${createStateRequest.name} UPDATED`;
        await stateHelper.updateState(createCountryRequest.id, createStateRequest.id, createStateRequest);

        // UPDATE region name
        createRegionRequest.name = `${createRegionRequest.name} UPDATED`;
        await regionHelper.updateRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createRegionRequest);

        // UPDATE subregion name
        createSubRegionRequest.name = `${createSubRegionRequest.name} UPDATED`;
        await subRegionHelper.updateSubRegion(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createSubRegionRequest);

        // Buildings should have updated country name and state name and region name and subregion name
        const retrieveBuildingResponse3 = await buildingHelper.getBuilding(createBuildingRequest2.countryId, createBuildingRequest2.stateId, createBuildingRequest2.regionId, createBuildingRequest2.subRegionId, createBuildingRequest2.id);
        expect(retrieveBuildingResponse3.data!.countryName).equals(createCountryRequest.name);
        expect(retrieveBuildingResponse3.data!.stateName).equals(createStateRequest.name);
        expect(retrieveBuildingResponse3.data!.regionName).equals(createRegionRequest.name);
        expect(retrieveBuildingResponse3.data!.subRegionName).equals(createSubRegionRequest.name);
        const retrieveBuildingResponse4 = await buildingHelper.getBuilding(createBuildingRequest2.countryId, createBuildingRequest2.stateId, createBuildingRequest2.regionId, createBuildingRequest2.subRegionId, createBuildingRequest2.id);
        expect(retrieveBuildingResponse4.data!.countryName).equals(createCountryRequest.name);
        expect(retrieveBuildingResponse4.data!.stateName).equals(createStateRequest.name);
        expect(retrieveBuildingResponse4.data!.regionName).equals(createRegionRequest.name);
        expect(retrieveBuildingResponse4.data!.subRegionName).equals(createSubRegionRequest.name);
    });

    it("Building - Retrieve with relation", async function (): Promise<void> {
        // CREATE building
        const building = await dbHelper.createTestBuilding();

        // CREATE facility
        await dbHelper.createTestFacility({ building });

        // RETRIEVE building
        const retrieveBuildingResponse = await buildingHelper.getBuilding(building.countryId, building.stateId, building.regionId, building.subRegionId, building.id);
        expect(retrieveBuildingResponse.data).to.not.be.undefined;

        const retrievedBuilding = retrieveBuildingResponse.data!;
        expect(retrievedBuilding.activeRelations).is.not.undefined;
        expect(retrievedBuilding.activeRelations!.length).equals(1);
        expect(retrievedBuilding.activeRelations![0].name).equals("Facility");
        expect(retrievedBuilding.activeRelations![0].activeCount).equals(1);
    });

    it("Building - Update name and check cascade update", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility();

        // RETRIEVE building
        const retrieveResponse = await buildingHelper.getBuilding(facility.countryId, facility.stateId, facility.regionId, facility.subRegionId, facility.buildingId);
        const retrievedBuilding = retrieveResponse.data!;

        // UPDATE building
        const updatedBuildingName = `${retrievedBuilding.name} - UPDATED`;
        retrievedBuilding.name = updatedBuildingName;
        const updateResponse = await buildingHelper.updateBuilding(retrievedBuilding.countryId, retrievedBuilding.stateId, retrievedBuilding.regionId, retrievedBuilding.subRegionId, retrievedBuilding.id, retrievedBuilding);
        expect(updateResponse.data.name).equals(updatedBuildingName);

        // RETRIEVE facility
        const updatedFacility = await dbHelper.getFacility(facility.id);
        expect(updatedFacility!.buildingName).equals(updatedBuildingName);
    });

    describe("Building Exceptions", async function (): Promise<void> {
        it("Building - Create - 409 Building Exists", async function (): Promise<void> {
            // CREATE test country, state, region, subregion and building
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
            const createBuildingRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test building",
                nameAbbreviated: "tb",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id,
                subRegionId: createSubRegionRequest.id
            };
            await buildingHelper.createBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest);

            // CREATE building again with same input (should conflict)
            await buildingHelper.createBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest, 409);
        });

        it("Building - Create - 400 Bad Request", async function (): Promise<void> {
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

            // CREATE test building with malformed input object
            const createBuildingRequest = {
                id_INVALID: LookupHelper.createTestGuid(),
                name: "test building",
                nameAbbreviated: "tb",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id,
                subRegionId: createSubRegionRequest.id
            };
            await buildingHelper.createBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, <any>createBuildingRequest, 400);
        });

        it("Building - Update - 404 Building Not Found", async function (): Promise<void> {
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

            // UPDATE building which doesn't exist
            const updateBuildingRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test building",
                nameAbbreviated: "tb",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id,
                subRegionId: createSubRegionRequest.id
            };
            await buildingHelper.updateBuilding(createCountryRequest.id, createStateRequest.id, updateBuildingRequest.regionId, updateBuildingRequest.subRegionId, updateBuildingRequest.id, updateBuildingRequest, 404);
        });

        it("Building - Update - 400 Bad Request", async function (): Promise<void> {
            // CREATE test country, state, region, subregion and building
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
            const createBuildingRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test building",
                nameAbbreviated: "tb",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id,
                subRegionId: createSubRegionRequest.id
            };
            await buildingHelper.createBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest);

            // UPDATE building with malformed input
            const updateBuildingRequest = {
                id: createBuildingRequest.id,
                name_INVALID: "test building",
                nameAbbreviated: "tb",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id,
                subRegionId: createSubRegionRequest.id
            };
            await buildingHelper.updateBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest.id, <any>updateBuildingRequest, 400);
        });

        it("Building - Update - 400 Path Parameter Mismatch", async function (): Promise<void> {
            // CREATE test country, state, region, subregion and building
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
            const createBuildingRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test building",
                nameAbbreviated: "tb",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id,
                subRegionId: createSubRegionRequest.id
            };
            await buildingHelper.createBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, createBuildingRequest);

            // UPDATE building with mismatched path parameter
            const updateBuildingRequest = {
                id: createBuildingRequest.id,
                name: "test building UPDATED",
                nameAbbreviated: "tb",
                countryId: createCountryRequest.id,
                stateId: createStateRequest.id,
                regionId: createRegionRequest.id,
                subRegionId: createSubRegionRequest.id
            };
            await buildingHelper.updateBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, "INVALID_BUILDING_ID", updateBuildingRequest, 400);
        });

        it("Building - Delete - 404 Building Not Found", async function (): Promise<void> {
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

            // DELETE building which doesn't exist
            await buildingHelper.deleteBuilding(createCountryRequest.id, createStateRequest.id, createRegionRequest.id, createSubRegionRequest.id, "UNKNOWN_BUILDING_ID", 404);
        });

        it("Building - Delete - 409 building linked to facility", async function (): Promise<void> {
            // CREATE building
            const building = await dbHelper.createTestBuilding();

            // CREATE facility
            await dbHelper.createTestFacility({ building });

            // DELETE building
            await buildingHelper.deleteBuilding(building.countryId, building.stateId, building.regionId, building.subRegionId, building.id, 409);
        });
    });
});