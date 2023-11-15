import { Settings } from "./settings-helper";
import { ClientSecretCredential } from "@azure/identity";
import { SecretClient } from "@azure/keyvault-secrets";

export class KeyvaultHelper {
    private static secretCache: { [key: string]: string } = {};
    private static secretClient: SecretClient;

    constructor() {
        const settings = new Settings();
        const creds = new ClientSecretCredential(settings.AutomationUserTenantId, settings.AutomationUserAppId, settings.AutomationUserSecret);
        KeyvaultHelper.secretClient = new SecretClient(`https://${settings.KeyvaultName}.vault.azure.net`, creds);
    }

    // Gets a keyvault secret with the specified name, or undefined if not found
    public async GetSecret(name: string): Promise<string | undefined> {
        const cached = KeyvaultHelper.secretCache[name];
        if (cached !== undefined) return cached;

        const secret = await KeyvaultHelper.secretClient.getSecret(name);
        if (secret !== undefined && secret.value !== undefined) {
            KeyvaultHelper.secretCache[name] = secret.value;
            return secret.value;
        }
        else {
            console.log(`Could not find secret '${name}'`);
            return undefined;
        }
    }

    public async GetEveryoneTeamId(): Promise<string> {
        return (await this.GetSecret("everyone-team-id"))!;
    }

    public async GetInfoTeamGeneralChannelId(): Promise<string> {
        return (await this.GetSecret("info-team-general-channel-id"))!;
    }

    public async GetDefaultRecorderUserPassword(): Promise<string> {
        return (await this.GetSecret("default-recorder-password"))!;
    }
}