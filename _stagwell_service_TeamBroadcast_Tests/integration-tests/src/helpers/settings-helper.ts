import dotenv from 'dotenv'
import { AxiosRequestConfig } from "axios";
import { trim } from 'lodash';

export class Settings {
  readonly AutomationUserAppId: string;
  readonly AutomationUserSecret: string;
  readonly AutomationUserTenantId: string;
  readonly APIMResourceId: string;
  readonly ServerUri: string;
  readonly CosmosConnectionString: string;
  readonly ApimProxyAppIdUri: string;
  readonly KeyvaultName: string;

  constructor() {
    dotenv.config();
    
    this.AutomationUserAppId = process.env["ARM_SP_APPID"] ?? "";
    this.AutomationUserSecret = process.env["ARM_SP_SECRET"] ?? "";
    this.AutomationUserTenantId = process.env["ARM_SP_TENANTID"] ?? "";
    this.APIMResourceId = process.env["APIM_RESOURCE_ID"] ?? "";
    this.ServerUri = process.env["SERVER_URI"] ?? "";
    this.CosmosConnectionString = process.env["COSMOS_CONNECTION_STR"] ?? "";
    this.ApimProxyAppIdUri = process.env["APIM_PROXY_APPID_URI"] ?? "";
    this.KeyvaultName = process.env["KEYVAULT_NAME"] ?? "";
  }

  public get APIMHeaders(): Record<string, string> {
    return {
      "Content-Type": "application/json",
    };
  }

  public get RequestConfig(): AxiosRequestConfig {
    return {
      headers: this.APIMHeaders,
      validateStatus: () => true,
    };
  }

  public BuildRequestUri(apiUriSuffix: string, path:string = '', apiVersion:string = 'v1'): string {
    const hasQuery = path.includes("?");
    return `${trim(this.ServerUri, '/')}/${trim(apiUriSuffix, '/')}${path ? `/${trim(path, '/')}`: path}${hasQuery ? "&" : "?"}api-version=${apiVersion}`;
  }
}
