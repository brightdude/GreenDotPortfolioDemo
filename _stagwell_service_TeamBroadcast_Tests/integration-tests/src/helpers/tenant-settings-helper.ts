import { CosmosDbHelper, IMeetingsChannelApp, ISetting, ISettingChannel } from "./cosmos-helper";

export class TenantSettingsHelper {
    private static settings: ISetting;

    private static async getSettings(): Promise<ISetting> {
        if (this.settings === undefined) {
            const dbHelper = new CosmosDbHelper();
            this.settings = await dbHelper.getSettings();
        }
        return this.settings;
    }

    public static async getTenantId(): Promise<string> {
        return (await this.getSettings()).tenantId;
    }

    public static async getDefaultUsageLocation(): Promise<string> {
        return (await this.getSettings()).defaultUsageLocation;
    }

    public static async getChannels(): Promise<ISettingChannel[]> {
        return (await this.getSettings()).channels;
    }

    public static async getChannelApps(): Promise<IMeetingsChannelApp[]> {
        return (await this.getSettings()).meetingsChannelApps;
    }
}