import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { TitleFixture } from "./title-fixture";
import { expect } from "chai";
import { AxiosResponse } from "axios";
import { ILookupItem } from "../../helpers/cosmos-helper";

const schemaProvider = new SchemaProvider();

export class TitleHelper {
  public async getTitles(): Promise<AxiosResponse<ILookupItem[]>> {
    const focusConnectApi = new TitleFixture();
    const getResponse = await focusConnectApi.getTitles();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupList), getResponse.data);
    return getResponse;
  }

  public async getTitle(titleId: string, expectedStatus: number = 200): Promise<AxiosResponse<ILookupItem | undefined>> {
    const focusConnectApi = new TitleFixture();
    const getResponse = await focusConnectApi.getTitle(titleId);
    expect(getResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupItem), getResponse.data);
    return getResponse;
  }

  public async createTitle(title: ILookupItem, expectedStatus: number = 201): Promise<AxiosResponse<ILookupItem>> {
    const focusConnectApi = new TitleFixture();
    console.log("Creating title " + title.id);
    const createResponse = await focusConnectApi.createTitle(title)
    expect(createResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 201)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupItem), createResponse.data);
    return createResponse;
  }

  public async updateTitle(titleId: string, title: ILookupItem, expectedStatus: number = 200): Promise<AxiosResponse<ILookupItem>> {
    const focusConnectApi = new TitleFixture();
    console.log("Updating title " + titleId);
    const updateResponse = await focusConnectApi.updateTitle(titleId, title);
    expect(updateResponse.status).to.equal(expectedStatus);

    if (expectedStatus == 200)
      await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupItem), updateResponse.data);
    return updateResponse;
  }

  public async deleteTitle(titleId: string, expectedStatus: number = 204): Promise<AxiosResponse<any>> {
    const focusConnectApi = new TitleFixture();
    console.log("Deleting title " + titleId);
    const delResponse = await focusConnectApi.deleteTitle(titleId);
    expect(delResponse.status).to.equal(expectedStatus);
    return delResponse;
  }
}