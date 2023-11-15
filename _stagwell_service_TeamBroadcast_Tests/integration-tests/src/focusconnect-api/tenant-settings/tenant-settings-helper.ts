import { SchemaProvider, focusConnectComponentSchemaSelection } from "../../schema-provider";
import { TenantSettingsFixture } from "./tenant-settings-fixture";
import { expect } from "chai";
import { ISettingAccessLevel, ISettingChannel } from "../../helpers/cosmos-helper";
import { AxiosResponse } from "axios";

const schemaProvider = new SchemaProvider();

export class TenantSettingsHelper {
  public async getAccessLevels(): Promise<AxiosResponse<ISettingAccessLevel[]>> {
    const settingsApi = new TenantSettingsFixture();
    const getResponse = await settingsApi.getAccessLevels();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.AccessLevelList), getResponse.data);
    return getResponse;
  }

  public async getChannels(): Promise<AxiosResponse<ISettingChannel[]>> {
    const settingsApi = new TenantSettingsFixture();
    const getResponse = await settingsApi.getChannels();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.ChannelList), getResponse.data);
    return getResponse;
  }

  public async getRecordingTypes(): Promise<AxiosResponse<any[]>> {
    const settingsApi = new TenantSettingsFixture();
    const getResponse = await settingsApi.getRecordingTypes();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupList), getResponse.data);
    return getResponse;
  }

  public async getStreamTypes(): Promise<AxiosResponse<any[]>> {
    const settingsApi = new TenantSettingsFixture();
    const getResponse = await settingsApi.getStreamTypes();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.LookupList), getResponse.data);
    return getResponse;
  }

  public async retrieveProvisioningStatusList(): Promise<AxiosResponse<any[]>> {
    const settingsApi = new TenantSettingsFixture();
    const getResponse = await settingsApi.retrieveProvisioningStatusList();
    expect(getResponse.status).to.equal(200);
    await schemaProvider.validateComponentSchema(await schemaProvider.GetfocusConnectComponentSchema(focusConnectComponentSchemaSelection.RecorderProvisioningStatusList), getResponse.data);
    return getResponse;
  }
}