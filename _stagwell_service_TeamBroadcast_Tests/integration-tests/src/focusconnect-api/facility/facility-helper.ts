import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { FacilityFixture } from "./facility-fixture";
import { expect } from "chai";
import { CosmosDbHelper, IBuilding, IFacility, IFacilityCreateUpdateParams } from "../../helpers/cosmos-helper";
import { AxiosResponse } from "axios";
import { LookupHelper } from "../lookup/lookup-helper";

const schemaProvider = new SchemaProvider();
const facilityFixture = new FacilityFixture();

export class FacilityHelper {
  public async createFacilityRequestBody(options?: { facilityId?: string, building?: IBuilding }): Promise<IFacilityCreateUpdateParams> {
    const dbHelper = new CosmosDbHelper();
    const building = options?.building ?? await dbHelper.createTestBuilding();
    const id = options?.facilityId ?? LookupHelper.createTestGuid();
    return {
      buildingId: building.id,
      displayName: `Facility ${id} team`,
      facilityType: "focusroom",
      floor: "13",
      id,
      room: "5B"
    };
  }

  public async getFacilities(): Promise<AxiosResponse<IFacility[]>> {
    const getResponse = await facilityFixture.getFacilities();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.FacilityList), getResponse.data);
    return getResponse;
  }

  public async getFacility(id: string, expectedStatus: number = 200): Promise<AxiosResponse<IFacility | undefined>> {
    const getResponse = await facilityFixture.getFacility(id);
    expect(getResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.Facility), getResponse.data);
    return getResponse;
  }

  public async createFacility(facilityRequest: IFacilityCreateUpdateParams, expectedStatus: number = 201): Promise<AxiosResponse<IFacility>> {
    console.log("Creating facility " + facilityRequest.id);
    const createResponse = await facilityFixture.createFacility(facilityRequest)
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.Facility), createResponse.data);
    return createResponse;
  }

  public async updateFacility(facilityId: string, facilityRequest: IFacilityCreateUpdateParams, expectedStatus: number = 200): Promise<AxiosResponse<IFacility>> {
    console.log("Updating facility " + facilityId);
    const updateResponse = await facilityFixture.updateFacility(facilityId, facilityRequest);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.Facility), updateResponse.data);
    return updateResponse;
  }

  public async deleteFacility(id: string, expectedStatus: number = 204): Promise<AxiosResponse<any>> {
    const delResponse = await facilityFixture.deleteFacility(id);
    expect(delResponse.status).to.equal(expectedStatus);
    return delResponse;
  }

  public async getTeamIdByChannelId(id: string, expectedStatus: number = 200): Promise<AxiosResponse<string>> {
    const getResponse = await facilityFixture.getTeamIdByChannelId(id);
    expect(getResponse.status).to.equal(expectedStatus);
    return getResponse;
  }
}