import { ClientCredentials, ModuleOptions, AccessToken } from "simple-oauth2"
import { Settings } from "./helpers/settings-helper";
import Axios from "axios";
import { expect } from "chai";
import Ajv from 'ajv';

export enum focusConnectComponentSchemaSelection {
  LookupItem = "lookupItem",
  LookupList = "TitlesGet200ApplicationJsonResponse",
  ChannelList = "SettingsChannelsGet200ApplicationJsonResponse",
  AccessLevelList = "SettingsAccessLevelsGet200ApplicationJsonResponse",
  CountryLocation = "countryLocation",
  CountryLocationList = "CountriesGet200ApplicationJsonResponse",
  StateLocation = "stateLocation",
  StateLocationList = "Countries-countryId-StatesGet200ApplicationJsonResponse",
  RegionLocation = "regionLocation",
  RegionLocationList = "Countries-countryId-States-stateId-RegionsGet200ApplicationJsonResponse",
  SubRegionLocation = "subRegionLocation",
  SubRegionLocationList = "Countries-countryId-States-stateId-Regions-regionId-SubRegionsGet200ApplicationJsonResponse",
  BuildingLocation = "buildingLocation",
  BuildingLocationList = "BuildingsGet200ApplicationJsonResponse",
  User = "user",
  UserList = "UsersGet200ApplicationJsonResponse",
  Recorder = "recorder",
  RecorderList = "RecordersGet200ApplicationJsonResponse",
  RecorderProvisioningStatusList = "SettingsProvisioningstatusvaluesGet200ApplicationJsonResponse",
  OnDemandMeeting = "onDemandMeeting",
  OnDemandMeetingList = "Teams-teamId-OnDemandMeetingsGet200ApplicationJsonResponse",
  OnDemandMeetingCreate = "onDemandMeetingCreateParams",
  ChatMessage = "chatMessage",
  HoldingCall = "holdingCall",
  CalendarHoldingCallList = "HoldingCallGet200ApplicationJsonResponse",
  CalendarHoldingCallsList = "HoldingCall-externalCalendarId-Get200ApplicationJsonResponse",
  MSJoinInfo = "msJoinInfo",
  Facility = "facility",
  FacilityList = "FacilitiesGet200ApplicationJsonResponse",
  Event = "event",
  EventList = "EventGet200ApplicationJsonResponse",
  Calendar = "calendar",
  CalendarList = "CalendarsGet200ApplicationJsonResponse"
}

export class SchemaProvider {
  private accessTokenPromise: Promise<AccessToken>;
  private readonly settings: Settings;
  private readonly focusConnectApi = new ApiSchema("virtual-hearing-focus-connect");

  constructor() {
    this.settings = new Settings();
    this.accessTokenPromise = this.initializeAccessTokenPromise();
  }

  private initializeAccessTokenPromise(): Promise<AccessToken> {
    const config: ModuleOptions = {
      client: {
        id: this.settings.AutomationUserAppId,
        secret: this.settings.AutomationUserSecret
      },
      auth: {
        tokenHost: `https://login.microsoftonline.com`,
        tokenPath: `/${this.settings.AutomationUserTenantId}/oauth2/token`
      }
    };
    const client = new ClientCredentials(config);
    return client.getToken({ resource: "https://management.azure.com/" });
  }

  async validateComponentSchema(schema: any, data: any) {
    expect(data).to.not.be.null;
    const ajv = new Ajv({ missingRefs: "ignore", unknownFormats: ["telephone"], format: false, schemaId: "auto", nullable: true });
    const valid = ajv.validate(schema, data);
    expect(valid, ajv.errorsText()).to.be.true; 
  }

  async GetfocusConnectComponentSchema(componentSchema: focusConnectComponentSchemaSelection) {
    return this.focusConnectApi.findSchema(componentSchema.toString(), await this.accessTokenPromise);    
  }
}

class ApiSchema {
  private apiName: string;
  private schema: object | null = null;
  private settings: Settings;

  constructor(apiName: string) {
    this.apiName = apiName;
    this.settings = new Settings();
  }

  private async loadApiSchema(accessToken: AccessToken): Promise<object> {
    const axiosConfig = { headers: { "Authorization": `Bearer ${accessToken.token.access_token}` } };
    //doco for this api: https://docs.microsoft.com/en-us/rest/api/apimanagement/2019-12-01/apischema/listbyapi
    const schemaResponse = await Axios.get(`https://management.azure.com${this.settings.APIMResourceId}/apis/${this.apiName}/schemas?api-version=2019-12-01`, axiosConfig);

    return this.schema = schemaResponse.data;
  }

  public async findSchema(componentSchemaName: string, accessToken: AccessToken): Promise<object> {
    try {
      if (this.schema == null)
        await this.loadApiSchema(accessToken);

      if (this.schema == null)
        throw new Error(`Could not load API schema for '${componentSchemaName}'`);

      // Find the particular schema we're looking for
      let schema = (<any>this.schema).value[0].properties.document.components.schemas[componentSchemaName];

      // Populate any referenced child schemas before returning
      return this.insertReferencedSchemas(schema);
    }
    catch (ex) {
      console.error(ex);
      throw ex;
    }
  }

  private insertReferencedSchemas(schema: any): any {
    if (schema.items === undefined || schema.items["$ref"] === undefined) return schema;

    const childSchemaName = schema.items['$ref'].split('/').pop();

    // Find the child schema based on the referenced name
    const childSchema = (<any>this.schema).value[0].properties.document.components.schemas[childSchemaName];
    if (childSchema !== undefined) {
      // Check if child schema has its own child schema(s)
      for (let propName of Object.keys(childSchema.properties)) {
        childSchema.properties[propName] = this.insertReferencedSchemas(childSchema.properties[propName]);
      }

      // Replace the $ref property with the child schema
      schema.items = childSchema;
    }

    return schema;
  }
}