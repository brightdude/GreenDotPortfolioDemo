import { expect } from "chai";
import { TenantSettingsHelper } from "./tenant-settings-helper";

const settingsHelper = new TenantSettingsHelper();

describe("TenantSettings", async function (): Promise<void> {

  before(async function () {
    console.log("TenantSettings Before...");
  });

  after(async function () {
    console.log("TenantSettings After...");
  });

  this.timeout(360000);

  it("Access Levels - Retrieve all", async function (): Promise<void> {
    const accessLevels = await settingsHelper.getAccessLevels();
    expect(accessLevels).to.not.be.empty;
  });

  it("Channels - Retrieve all", async function (): Promise<void> {
    const channels = await settingsHelper.getChannels();
    expect(channels).to.not.be.empty;
  });

  it("Recording Types - Retrieve all", async function (): Promise<void> {
    const recordingTypes =  await settingsHelper.getRecordingTypes();
    expect(recordingTypes).to.not.be.empty;
  });

  it("Stream Types - Retrieve all", async function (): Promise<void> {
    const streamTypes = await settingsHelper.getStreamTypes();
    expect(streamTypes).to.not.be.empty;
  });

  it("Recorder Provisioning Status Values - Retrieve all", async function (): Promise<void> {
    const provisioningStatusValues = await settingsHelper.retrieveProvisioningStatusList();
    expect(provisioningStatusValues).to.not.be.empty;
  });
});
