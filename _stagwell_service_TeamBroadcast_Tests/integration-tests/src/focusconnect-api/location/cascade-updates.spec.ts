import { expect } from "chai";
import { CosmosDbHelper, IRegion, IState } from "../../helpers/cosmos-helper";
import { FacilityHelper } from "../facility/facility-helper";
import { BuildingHelper } from "../location/building-helper";
import { CountryHelper } from "../location/country-helper";
import { RegionHelper } from "../location/region-helper";
import { StateHelper } from "../location/state-helper";
import { SubRegionHelper } from "../location/subregion-helper";
import { LookupHelper } from "../lookup/lookup-helper";

const countryHelper = new CountryHelper();
const stateHelper = new StateHelper();
const facilityHelper = new FacilityHelper();
const subRegionHelper = new SubRegionHelper();
const regionHelper = new RegionHelper();
const buildingHelper = new BuildingHelper();
const dbHelper = new CosmosDbHelper();

describe("Cascade Updates", async function (): Promise<void> {
  before(async function () {
    console.log("State Before...");
  });

  after(async function () {
    console.log("After...");
    await dbHelper.deleteTestData(false, ["Locations"]);
  });

  this.timeout(360000);

  const setupTestLocations = async (
    createCountryId: string,
    createStateId: string,
    createRegionId: string,
    createSubRegionId: string,
    createBuildingId: string
  ) => {
    const createCountryRequest1 = {
      id: createCountryId,
      name: "test country 1",
    };
    await countryHelper.createCountry(createCountryRequest1);

    const createStateRequest1: IState = {
      id: createStateId,
      name: "test state 1",
      countryId: createCountryRequest1.id,
      status: "Active"
    };

    await stateHelper.createState(
      createCountryRequest1.id,
      createStateRequest1
    );

    const createRegionRequest1 = {
      id: createRegionId,
      name: "test region 1",
      nameAbbreviated: "tr1",
      countryId: createCountryRequest1.id,
      stateId: createStateRequest1.id,
    };
    await regionHelper.createRegion(
      createRegionRequest1.countryId,
      createRegionRequest1.stateId,
      createRegionRequest1
    );

    const createSubRegionRequest1 = {
      id: createSubRegionId,
      name: "test subregion 1",
      nameAbbreviated: "tsr1",
      countryId: createCountryRequest1.id,
      stateId: createStateRequest1.id,
      regionId: createRegionRequest1.id,
    };

    await subRegionHelper.createSubRegion(
      createSubRegionRequest1.countryId,
      createSubRegionRequest1.stateId,
      createSubRegionRequest1.regionId,
      createSubRegionRequest1
    );

    const createBuildingRequest1 = {
      id: createBuildingId,
      name: "test building 1",
      nameAbbreviated: "tb1",
      countryId: createCountryRequest1.id,
      stateId: createStateRequest1.id,
      regionId: createRegionRequest1.id,
      subRegionId: createSubRegionRequest1.id,
    };

    await buildingHelper.createBuilding(
      createBuildingRequest1.countryId,
      createBuildingRequest1.stateId,
      createBuildingRequest1.regionId,
      createBuildingRequest1.subRegionId,
      createBuildingRequest1
    );
  };

  it("Country changes name with Facility", async function (): Promise<void> {
    const createdFacility = await dbHelper.createTestFacility();

    const newCountryName = "New Country Name";
    const facilityCountry = (
      await countryHelper.getCountry(createdFacility.countryId)
    ).data!;
    facilityCountry.name = newCountryName;
    await countryHelper.updateCountry(facilityCountry.id, facilityCountry);

    const retrievedStateResponse = stateHelper.getState(
      createdFacility.countryId,
      createdFacility.stateId
    );
    const retrieveRegionResponse = regionHelper.getRegion(
      createdFacility.countryId,
      createdFacility.stateId,
      createdFacility.regionId
    );
    const retrievedSubRegionResponse = subRegionHelper.getSubRegion(
      createdFacility.countryId,
      createdFacility.stateId,
      createdFacility.regionId,
      createdFacility.subRegionId
    );
    const retrievedBuildingResponse = buildingHelper.getBuilding(
      createdFacility.countryId,
      createdFacility.stateId,
      createdFacility.regionId,
      createdFacility.subRegionId,
      createdFacility.buildingId
    );
    const retrievedFacilityResponse = facilityHelper.getFacility(
      createdFacility.id
    );

    const retrievedState = (await retrievedStateResponse).data!;
    const retrievedRegion = (await retrieveRegionResponse).data!;
    const retrievedSubRegion = (await retrievedSubRegionResponse).data!;
    const retrievedBuilding = (await retrievedBuildingResponse).data!;
    const retrievedFacility = (await retrievedFacilityResponse).data!;
    expect(retrievedState.countryName).equal(newCountryName);
    expect(retrievedRegion.countryName).equal(newCountryName);
    expect(retrievedSubRegion.countryName).equal(newCountryName);
    expect(retrievedBuilding.countryName).equal(newCountryName);
    expect(retrievedFacility.countryName).equal(newCountryName);
  });

  it("State changes Country", async function (): Promise<void> {
    const createCountryId = LookupHelper.createTestGuid();
    const createStateId = LookupHelper.createTestGuid();
    const createRegionId = LookupHelper.createTestGuid();
    const createSubRegionId = LookupHelper.createTestGuid();
    const createBuildingId = LookupHelper.createTestGuid();

    await setupTestLocations(
      createCountryId,
      createStateId,
      createRegionId,
      createSubRegionId,
      createBuildingId
    );

    //2nd test country
    const newCountryRequest = {
      id: LookupHelper.createTestGuid(),
      name: "test country 2",
    };
    await countryHelper.createCountry(newCountryRequest);

    const updateStateReqBody: IState = {
      countryId: newCountryRequest.id,
      id: createStateId,
      name: "State With New Country",
      type: "state",
      status: "Active"
    };

    // Assign new Country to State
    const updatedState = (await stateHelper.updateState(
      newCountryRequest.id,
      createStateId,
      updateStateReqBody
    )).data!;

    const retrieveRegionResponse = regionHelper.getRegion(
      newCountryRequest.id,
      createStateId,
      createRegionId
    );
    const retrievedSubRegionResponse = subRegionHelper.getSubRegion(
      newCountryRequest.id,
      createStateId,
      createRegionId,
      createSubRegionId
    );
    const retrievedBuildingResponse = buildingHelper.getBuilding(
      newCountryRequest.id,
      createStateId,
      createRegionId,
      createSubRegionId,
      createBuildingId
    );

    const retrievedRegion = (await retrieveRegionResponse).data!;
    const retrievedSubRegion = (await retrievedSubRegionResponse).data!;
    const retrievedBuilding = (await retrievedBuildingResponse).data!;

    expect(retrievedRegion.countryName).equal(newCountryRequest.name);
    expect(retrievedRegion.stateName).equal(updatedState.name);

    expect(retrievedSubRegion.countryId).equal(newCountryRequest.id);
    expect(retrievedSubRegion.countryName).equal(newCountryRequest.name);
    expect(retrievedSubRegion.stateName).equal(updatedState.name);

    expect(retrievedBuilding.countryId).equal(newCountryRequest.id);
    expect(retrievedBuilding.countryName).equal(newCountryRequest.name);
    expect(retrievedBuilding.stateName).equal(updatedState.name);

    // State has Name changed
    expect(updatedState.name).equal(updateStateReqBody.name);
  });

  it("Region changes State", async function (): Promise<void> {
    const createCountryId = LookupHelper.createTestGuid();
    const createStateId = LookupHelper.createTestGuid();
    const createRegionId = LookupHelper.createTestGuid();
    const createSubRegionId = LookupHelper.createTestGuid();
    const createBuildingId = LookupHelper.createTestGuid();

    await setupTestLocations(
      createCountryId,
      createStateId,
      createRegionId,
      createSubRegionId,
      createBuildingId
    );

    //2nd State
    const newStateRequest: IState = {
      id: LookupHelper.createTestGuid(),
      name: "New State",
      countryId: createCountryId,
      status: "Active"
    };

    const newState = (
      await stateHelper.createState(createCountryId, newStateRequest)
    ).data!;

    // Assign new state to Region
    const updateRegionBody: IRegion = {
      countryId: createCountryId,
      stateId: newStateRequest.id,
      id: createRegionId,
      name: "Region with New State",
      status: "Active",
      type: "region",
    };

    // Assign new state to region
    await regionHelper.updateRegion(
      createCountryId,
      newStateRequest.id,
      createRegionId,
      updateRegionBody
    );

    // Get with new Id
    const retrieveRegionResponse = regionHelper.getRegion(
      createCountryId,
      newStateRequest.id,
      createRegionId
    );
    const retrievedSubRegionResponse = subRegionHelper.getSubRegion(
      createCountryId,
      newStateRequest.id,
      createRegionId,
      createSubRegionId
    );
    const retrievedBuildingResponse = buildingHelper.getBuilding(
      createCountryId,
      newStateRequest.id,
      createRegionId,
      createSubRegionId,
      createBuildingId
    );

    const retrievedRegion = (await retrieveRegionResponse).data!;
    const retrievedSubRegion = (await retrievedSubRegionResponse).data!;
    const retrievedBuilding = (await retrievedBuildingResponse).data!;

    expect(retrievedRegion.name).equal(updateRegionBody.name);
    expect(retrievedRegion.countryName).equal(newState.countryName);
    expect(retrievedRegion.stateName).equal(newState.name);

    expect(retrievedSubRegion.countryName).equal(newState.countryName);
    expect(retrievedSubRegion.stateName).equal(newState.name);
    expect(retrievedSubRegion.regionName).equal(retrievedRegion.name);

    expect(retrievedBuilding.countryName).equal(newState.countryName);
    expect(retrievedBuilding.stateName).equal(newState.name);
    expect(retrievedBuilding.regionName).equal(retrievedRegion.name);
    expect(retrievedBuilding.subRegionName).equal(retrievedSubRegion.name);
    expect(retrievedBuilding.regionName).equal(retrievedRegion.name);
  });

  it("SubRegion changes Region with new Country", async function (): Promise<void> {

    const createCountryId = LookupHelper.createTestGuid();
    const createStateId = LookupHelper.createTestGuid();
    const createRegionId = LookupHelper.createTestGuid();
    const createSubRegionId = LookupHelper.createTestGuid();
    const createBuildingId = LookupHelper.createTestGuid();

    const createCountryId2 = LookupHelper.createTestGuid();
    const createStateId2 = LookupHelper.createTestGuid();
    const createRegionId2 = LookupHelper.createTestGuid();
    const createSubRegionId2 = LookupHelper.createTestGuid();
    const createBuildingId2 = LookupHelper.createTestGuid();

    await setupTestLocations(
      createCountryId,
      createStateId,
      createRegionId,
      createSubRegionId,
      createBuildingId
    );
    
    await setupTestLocations(
      createCountryId2,
      createStateId2,
      createRegionId2,
      createSubRegionId2,
      createBuildingId2
    );

    const updateSubRegionBody = {
      countryId: createCountryId2,
      stateId: createStateId2,
      regionId: createRegionId2,
      id: createSubRegionId,
      name: "SubRegion with New region new Country",
      status: "Active",
      type: "subregion",
    };

    
    // Assign new Region to Sub region
    await subRegionHelper.updateSubRegion(
      createCountryId2,
      createStateId2,
      createRegionId2,
      createSubRegionId,
      updateSubRegionBody
    );

    const retrievedNewRegion = (await regionHelper.getRegion(createCountryId2, createStateId2, createRegionId2)).data!;
    
    const retrievedSubRegionResponse = subRegionHelper.getSubRegion(
      createCountryId2,
      createStateId2,
      createRegionId2,
      createSubRegionId
    );

    const retrievedBuildingResponse = buildingHelper.getBuilding(
      createCountryId2,
      createStateId2,
      createRegionId2,
      createSubRegionId,
      createBuildingId
    );

    const retrievedSubRegion = (await retrievedSubRegionResponse).data!;
    const retrievedBuilding = (await retrievedBuildingResponse).data!;

    expect(retrievedSubRegion.countryName).equal(retrievedNewRegion.countryName);
    expect(retrievedSubRegion.stateName).equal(retrievedNewRegion.stateName);
    expect(retrievedSubRegion.regionName).equal(retrievedNewRegion.name);
    expect(retrievedSubRegion.name).equal(updateSubRegionBody.name);

    expect(retrievedBuilding.countryName).equal(retrievedNewRegion.countryName);
    expect(retrievedBuilding.stateName).equal(retrievedNewRegion.stateName);
    expect(retrievedBuilding.regionName).equal(retrievedNewRegion.name);
    expect(retrievedBuilding.subRegionName).equal(updateSubRegionBody.name);
  });

  it("Building changes SubRegion with Facility", async function (): Promise<void> {
    const createCountryId = LookupHelper.createTestGuid();
    const createStateId = LookupHelper.createTestGuid();
    const createRegionId = LookupHelper.createTestGuid();
    const createSubRegionId = LookupHelper.createTestGuid();
    const createBuildingId = LookupHelper.createTestGuid();
    const createdFacility = await dbHelper.createTestFacility();

    // Create an additional set of locations
    await setupTestLocations(
      createCountryId,
      createStateId,
      createRegionId,
      createSubRegionId,
      createBuildingId
    );

    const facilityBuilding = (
      await buildingHelper.getBuilding(
        createdFacility.countryId,
        createdFacility.stateId,
        createdFacility.regionId,
        createdFacility.subRegionId,
        createdFacility.buildingId
      )
    ).data!;

    // Change the Facility's building Sub region
    facilityBuilding.countryId = createCountryId;
    facilityBuilding.stateId = createStateId;
    facilityBuilding.regionId = createRegionId;
    facilityBuilding.subRegionId = createSubRegionId;
    facilityBuilding.name = "Building new SubRegion";

    const updatedBuilding = (
      await buildingHelper.updateBuilding(
        createCountryId,
        createStateId,
        createRegionId,
        createSubRegionId,
        createdFacility.buildingId,
        facilityBuilding
      )
    ).data!;

    //
    const retrievedSubRegion = (
      await subRegionHelper.getSubRegion(
        createCountryId,
        createStateId,
        createRegionId,
        createSubRegionId
      )
    ).data!;

    //Facility reflects Sub region change
    const retrievedFacility = (
      await facilityHelper.getFacility(createdFacility.id)
    ).data!;
    expect(retrievedFacility.countryId).equal(retrievedSubRegion.countryId);
    expect(retrievedFacility.countryName).equal(retrievedSubRegion.countryName);
    expect(retrievedFacility.stateId).equal(retrievedSubRegion.stateId);
    expect(retrievedFacility.stateName).equal(retrievedSubRegion.stateName);
    expect(retrievedFacility.regionId).equal(retrievedSubRegion.regionId);
    expect(retrievedFacility.regionName).equal(retrievedSubRegion.regionName);
    expect(retrievedFacility.subRegionId).equal(retrievedSubRegion.id);
    expect(retrievedFacility.subRegionName).equal(retrievedSubRegion.name);

    expect(updatedBuilding.countryName).equal(retrievedSubRegion.countryName);
    expect(updatedBuilding.stateName).equal(retrievedSubRegion.stateName);
    expect(updatedBuilding.regionName).equal(retrievedSubRegion.regionName);
    expect(updatedBuilding.subRegionName).equal(retrievedSubRegion.name);

  });

  it("Region changes State with new Country, with Facility", async function (): Promise<void> {
    const createdFacility = await dbHelper.createTestFacility();
    const newCountry = (await dbHelper.createTestCountry()).item;

    const newStateRequest: IState = {
      id: LookupHelper.createTestGuid(),
      name: "New State",
      countryId: newCountry.id,
      status: "Active"
    };

    // New State in new country
    const newState = (
      await stateHelper.createState(newCountry.id, newStateRequest)
    ).data!;

    const retrievedFacilityRegion = (
      await regionHelper.getRegion(
        createdFacility.countryId,
        createdFacility.stateId,
        createdFacility.regionId
      )
    ).data!;

    const updateFacilityRegionBody = {
      countryId: newCountry.id,
      stateId: newState.id,
      id: retrievedFacilityRegion.id,
      name: "Region with New State and Country",
      type: "region",
    };

    // Assign new state to region
    const updatedRegion = (
      await regionHelper.updateRegion(
        newCountry.id,
        newState.id,
        retrievedFacilityRegion.id,
        updateFacilityRegionBody
      )
    ).data;

    const retrievedSubRegionResponse = subRegionHelper.getSubRegion(
      newCountry.id,
      newState.id,
      updatedRegion.id,
      createdFacility.subRegionId
    );

    const retrievedBuildingResponse = buildingHelper.getBuilding(
      newCountry.id,
      newState.id,
      updatedRegion.id,
      createdFacility.subRegionId,
      createdFacility.buildingId
    );

    const retrievedSubRegion = (await retrievedSubRegionResponse).data!;
    const retrievedBuilding = (await retrievedBuildingResponse).data!;
    const retrievedFacility = await dbHelper.getFacility(createdFacility.id);

    //Regions
    expect(updatedRegion.countryId).not.eql(retrievedFacilityRegion.countryId);
    expect(updatedRegion.name).equal(updateFacilityRegionBody.name);
    expect(updatedRegion.countryName).equal(newState.countryName);
    expect(updatedRegion.stateName).equal(newState.name);

    //Sub Regions
    expect(retrievedSubRegion.countryName).equal(newState.countryName);
    expect(retrievedSubRegion.stateName).equal(newState.name);
    expect(retrievedSubRegion.regionName).equal(updatedRegion.name);

    //Building
    expect(retrievedBuilding.countryName).equal(newState.countryName);
    expect(retrievedBuilding.stateName).equal(newState.name);
    expect(retrievedBuilding.regionName).equal(updatedRegion.name);
    expect(retrievedBuilding.subRegionName).equal(retrievedSubRegion.name);
    
    // Facility
    expect(retrievedFacility?.countryId).equal(newState.countryId);
    expect(retrievedFacility?.countryName).equal(newState.countryName);
    expect(retrievedFacility?.stateId).equal(newState.id);
    expect(retrievedFacility?.stateName).equal(newState.name);
    expect(retrievedFacility?.regionId).equal(updatedRegion.id);
    expect(retrievedFacility?.regionName).equal(updatedRegion.name);
    expect(retrievedFacility?.subRegionId).equal(retrievedSubRegion.id);
    expect(retrievedFacility?.subRegionName).equal(retrievedSubRegion.name);
  });
});
