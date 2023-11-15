import chai, { expect } from "chai";
import chaiAsPromised from "chai-as-promised";
import { CountryHelper } from "./country-helper";
import { LookupHelper } from "../lookup/lookup-helper";
import { StateHelper } from "./state-helper";
import { CosmosDbHelper } from "../../helpers/cosmos-helper";

chai.use(chaiAsPromised);

const countryHelper = new CountryHelper();
const stateHelper = new StateHelper();
const dbHelper = new CosmosDbHelper();

describe("Country", async function (): Promise<void> {

    before(async function () {
        console.log("Country Before...");
    });

    after(async function () {
        console.log("Country After...");
        await dbHelper.deleteTestData(false, ["Locations"]);
    });

    this.timeout(360000);

    it("Country - Retrieve all", async function (): Promise<void> {
        const createCountryRequest = {
            id: LookupHelper.createTestGuid(),
            name: "test country"
        };
        await countryHelper.createCountry(createCountryRequest);

        const retrieveResponse1 = await countryHelper.getCountries();
        const idList1 = retrieveResponse1.data.map(c => c.id);
        expect(idList1).to.contain(createCountryRequest.id);

        await countryHelper.deleteCountry(createCountryRequest.id);

        const retrieveResponse2 = await countryHelper.getCountries();
        const idList2 = retrieveResponse2.data.map(c => c.id);
        expect(idList2).to.not.contain(createCountryRequest.id);
    });

    it("Country - Create, Update, Retrieve and Delete", async function (): Promise<void> {
        // CREATE
        const createCountryRequest = {
            id: LookupHelper.createTestGuid(),
            name: "test country"
        };
        const createCountryResponse = await countryHelper.createCountry(createCountryRequest);
        expect(createCountryResponse.data.id).equals(createCountryRequest.id);
        expect(createCountryResponse.data.name).equals(createCountryRequest.name);
        expect(createCountryResponse.data.status).equals("Active");
        expect(createCountryResponse.data.type).equals("country");

        // UPDATE
        createCountryRequest.name = createCountryRequest.name + " - UPDATED";
        const updateCountryResponse = await countryHelper.updateCountry(createCountryRequest.id, createCountryRequest);
        expect(updateCountryResponse.data.id).equals(createCountryRequest.id);
        expect(updateCountryResponse.data.name).equals(createCountryRequest.name);
        expect(updateCountryResponse.data.status).equals("Active");
        expect(updateCountryResponse.data.type).equals("country");

        // RETRIEVE
        const retrieveCountryResponse1 = await countryHelper.getCountry(createCountryRequest.id);
        const retrievedCountry1 = retrieveCountryResponse1.data!;
        expect(retrievedCountry1.id).equals(createCountryRequest.id);
        expect(retrievedCountry1.name).equals(createCountryRequest.name);
        expect(retrievedCountry1.status).equals("Active");
        expect(retrievedCountry1.type).equals("country");
        expect(retrievedCountry1.activeRelations?.length).equals(1);
        expect(retrievedCountry1.activeRelations![0].name).equals("State");
        expect(retrievedCountry1.activeRelations![0].activeCount).equals(0);

        // DELETE
        await countryHelper.deleteCountry(createCountryRequest.id);

        // RETRIEVE
        const retrieveCountryResponse2 = await countryHelper.getCountry(createCountryRequest.id);
        expect(retrieveCountryResponse2.data!.status).equals("Deleted");
    });

    it("Country - Retrieve with relation", async function (): Promise<void> {
        // CREATE building
        const building = await dbHelper.createTestBuilding();

        // RETRIEVE country
        const retrieveCountryResponse = await countryHelper.getCountry(building.countryId);
        expect(retrieveCountryResponse.data).to.not.be.undefined;

        const retrievedCountry = retrieveCountryResponse.data!;
        expect(retrievedCountry.activeRelations).is.not.undefined;
        expect(retrievedCountry.activeRelations!.length).equals(1);
        expect(retrievedCountry.activeRelations![0].name).equals("State");
        expect(retrievedCountry.activeRelations![0].activeCount).equals(1);
    });

    it("Country - Update name and check cascade update", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility();

        // RETRIEVE country
        const retrieveResponse = await countryHelper.getCountry(facility.countryId);
        const retrievedCountry = retrieveResponse.data!;

        // UPDATE country
        const updatedCountryName = `${retrievedCountry.name} - UPDATED`;
        retrievedCountry.name = updatedCountryName;
        const updateResponse = await countryHelper.updateCountry(retrievedCountry.id, retrievedCountry);
        expect(updateResponse.data.name).equals(updatedCountryName);

        // RETRIEVE facility
        const updatedFacility = await dbHelper.getFacility(facility.id);
        expect(updatedFacility!.countryName).equals(updatedCountryName);
    });

    describe("Country Exceptions", async function (): Promise<void> {
        it("Country - Create - 409 Country Exists", async function (): Promise<void> {
            const createCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(createCountryRequest);
            await countryHelper.createCountry(createCountryRequest, 409);
        });

        it("Country - Create - 400 Bad Request", async function (): Promise<void> {
            const createCountryRequest = {
                idzzz: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(<any>createCountryRequest, 400);
        });

        it("Country - Update - 404 Country Not Found", async function (): Promise<void> {
            const updateCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.updateCountry(updateCountryRequest.id, updateCountryRequest, 404);
        });

        it("Country - Update - 400 Bad Request", async function (): Promise<void> {
            const createCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(createCountryRequest);
            const updateCountryRequest = {
                id: createCountryRequest.id,
                namezzz: "test country"
            };
            await countryHelper.updateCountry(updateCountryRequest.id, <any>updateCountryRequest, 400);
        });

        it("Country - Update - 400 Path Parameter Mismatch", async function (): Promise<void> {
            const createCountryRequest = {
                id: LookupHelper.createTestGuid(),
                name: "test country"
            };
            await countryHelper.createCountry(createCountryRequest);
            const updateCountryRequest = {
                id: createCountryRequest.id,
                name: "test country UPDATED"
            };
            await countryHelper.updateCountry("INVALID_COUNTRY_ID", updateCountryRequest, 400);
        });

        it("Country - Delete - 404 Country Not Found", async function (): Promise<void> {
            await countryHelper.deleteCountry("UNKNOWN_COUNTRY_ID", 404);
        });

        it("Country - Delete - 409 active children exist", async function (): Promise<void> {
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

            // DELETE
            await countryHelper.deleteCountry(createCountryRequest.id, 409);
        });
    });
});