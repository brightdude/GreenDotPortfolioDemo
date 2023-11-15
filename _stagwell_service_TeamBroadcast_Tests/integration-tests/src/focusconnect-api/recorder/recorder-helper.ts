import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { IUpdateRecorderRequest, RecorderFixture } from "./recorder-fixture";
import { expect } from "chai";
import { ILookupItem, IRecorder } from "../../helpers/cosmos-helper";
import { Guid } from "guid-typescript";

const schemaProvider = new SchemaProvider();

export class RecorderHelper {
  public async retrieveRecorderList(expectedStatus: number = 200): Promise<any> {
    const focusApi = new RecorderFixture();
    let retrieveResponse = await focusApi.retrieveRecorders();
    expect(retrieveResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.RecorderList), retrieveResponse.data);
    return retrieveResponse;
  }

  public async getRecorder(id: string, expectedStatus: number = 200): Promise<any> {
    const focusApi = new RecorderFixture();
    let retrieveResponse = await focusApi.getRecorder(id);
    expect(retrieveResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.Recorder), retrieveResponse.data);
    return retrieveResponse;
  }

  public async createRecorder(department: ILookupItem, expectedStatus: number = 201): Promise<IRecorder> {
    const guid = Guid.raw();
    const focusApi = new RecorderFixture();
    const response = await focusApi.createRecorder({
      departmentId: department.id,
      displayName: `__test_Test Recorder ${guid}`,
      locationName: `__test_Test_Location_${guid}`,
      recordingTypeId: "2",
      streamTypeId: "2"
    });
    expect(response.status).to.equal(expectedStatus);
    console.log(`Recorder created ${response.data.displayName}`);
    return response.data;
  }

  public async updateRecorder(id: string, request: IUpdateRecorderRequest, expectedStatus: number = 200): Promise<IRecorder> {   
    const focusApi = new RecorderFixture();
    const response = await focusApi.updateRecorder(id, request);
    expect(response.status).to.equal(expectedStatus);
    console.log(`Recorder updated ${response.data.displayName}`);
    return response.data;
  }

  public async deleteRecorder(id: string, expectedStatus: number = 204): Promise<IRecorder> {   
    const focusApi = new RecorderFixture();
    const response = await focusApi.deleteRecorder(id);
    expect(response.status).to.equal(expectedStatus);
    console.log(`Recorder deleted ${id}`);
    return response.data;
  }
}