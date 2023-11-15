import { CosmosClient, Database, OperationInput, OperationResponse } from '@azure/cosmos';
import { isEmpty } from 'lodash';
import moment from 'moment';
import { LookupHelper } from '../focus-connect-api/lookup/lookup-helper';
import { AadConversationMember, ConversationMember, GraphAccessUserType, GraphHelper, Invitation, OnlineMeeting } from './graph-helper';
import { Settings } from "./settings-helper";
import { KeyvaultHelper } from "./keyvault-helper";
import { TenantSettingsHelper } from './tenant-settings-helper';

export class CosmosDbHelper {
  private _database: Database;

  constructor() {
    const settings = new Settings();
    const client = new CosmosClient(settings.CosmosConnectionString)
    this._database = client.database("focusConnect-DB");
  }

  async getTitle(id: string): Promise<ILookupItem | undefined> {
    const list = await this.executeQuery("Titles", `SELECT * FROM t WHERE t.id = '${id}'`, id);
    return list.length === 0 ? undefined : list[0];
  }

  async getRole(id: string): Promise<ILookupItem | undefined> {
    const list = await this.executeQuery("PersonnelRoles", `SELECT * FROM r WHERE r.id = '${id}'`, id);
    return list.length === 0 ? undefined : list[0];
  }

  async getDepartment(id: string): Promise<ILookupItem | undefined> {
    const list = await this.executeQuery("Departments", `SELECT * FROM d WHERE d.id = '${id}'`, id);
    return list.length === 0 ? undefined : list[0];
  }

  async getSettings(id: string = "TenantSettings"): Promise<ISetting> {
    const result = await this.executeQuery(
      "Settings",
      `SELECT s.id, s.tenantId, s.defaultUsageLocation, s.channels, s.meetingsChannelApps, s.accessLevelDescriptions FROM s where s.id = '${id}'`);
    return isEmpty(result) ? undefined : result[0];
  }


  async createTestFacility(options?: { building?: IBuilding, createTeam?: boolean }): Promise<IFacility> {
    const facilityId = LookupHelper.createTestGuid();
    const facilityName = `Facility ${facilityId} team`;
    const building = options?.building ?? await this.createTestBuilding();

    const team = (options?.createTeam) ? (await this.createMsTeam(facilityName)) : {
      msTeamId: LookupHelper.createTestGuid(),
      name: facilityName,
      type: "facility",
      apps: [],
      channels: [{
        addRecorderUser: false,
        isDefaultChannel: false,
        msChannelId: `19:${LookupHelper.createTestGuid().replace(/-/g, "")}@thread.tacv2`,
        name: "focusroom"
      },
      {
        addRecorderUser: false,
        isDefaultChannel: false,
        msChannelId: `19:${LookupHelper.createTestGuid().replace(/-/g, "")}@thread.tacv2`,
        name: "Meetings"
      }]
    };

    const facility: IFacility = {
      id: facilityId,
      displayName: `__test_Test Facility ${facilityId}`,
      facilityType: "focusroom",
      floor: "13",
      room: "5B",
      team,
      countryId: building.countryId,
      countryName: building.countryName!,
      stateId: building.stateId,
      stateName: building.stateName!,
      regionId: building.regionId,
      regionName: building.regionName!,
      regionAbbreviated: building.nameAbbreviated!,
      subRegionId: building.subRegionId,
      subRegionName: building.subRegionName!,
      subRegionAbbreviated: building.nameAbbreviated!,
      buildingId: building.id,
      buildingName: building.name,
      buildingAbbreviated: building.nameAbbreviated!
    };
    console.log(`Creating test facility ${facility.displayName}...`);
    await this._database.container("Facilities").items.create(facility);
    return facility;
  }

  private async createMsTeam(facilityName: string): Promise<IFacilityTeam> {
    const graphHelper = new GraphHelper();
    const msTeamId = await graphHelper.TeamsCreate(facilityName, "facility");
    const channels = await TenantSettingsHelper.getChannels();
    const graphChannels = await graphHelper.TeamChannelsCreate(msTeamId, channels);
    return {
      msTeamId,
      name: facilityName,
      type: "facility",
      apps: [],
      channels: graphChannels.filter(c => c !== undefined).map(c => {
        return {
          addRecorderUser: c.displayName === "General" ? false : true,
          isDefaultChannel: c.displayName === "General" ? false : c.isFavoriteByDefault,
          msChannelId: c.id,
          name: c.displayName
        }
      })
    };
  }

  async getFacility(id: string): Promise<IFacility | undefined> {
    const result = await this.executeQuery(
      'Facilities',
      `SELECT * FROM f WHERE f.id = '${id}'`,
      id
    );
    return isEmpty(result) ? undefined : result[0];
  }

  async listCalendars(): Promise<ICalendar[]> {
    return await this.executeQuery(
      'Calendars',
      `SELECT * FROM c`
    );
  }

  async getCalendar(externalCalendarId: string): Promise<ICalendar | undefined> {
    const result = await this.executeQuery(
      'Calendars',
      `SELECT * FROM c WHERE c.externalCalendarId = '${externalCalendarId}'`
    );
    return isEmpty(result) ? undefined : result[0];
  }

  async createTestCalendar(options?: { facility?: IFacility, focusUsers?: IUser[], addUsersToTeam?: boolean, department?: ILookupItem, recorders?: IRecorder[] }): Promise<ICalendar> {
    // Create the lookup objects
    const facility = options?.facility ?? (await this.createTestFacility());
    const focusUsers = options?.focusUsers ?? [(await this.createTestUser())];
    const recorders = options?.recorders ?? [(await this.createTestRecorder())];
    const department = options?.department ?? await this.createTestLookup("Departments", "department");

    // Create the calendar object
    const extId = (Math.random() * 9999).toFixed(0).padStart(4, "0");
    const calendar: ICalendar = {
      calendarName: `__test_Test Calendar ${extId}`,
      focusUsers: focusUsers.map(u => u.email),
      departmentId: department.id,
      departmentName: department.name,
      externalCalendarId: `cal${extId}`,
      facilityId: facility.id,
      msTeamId: facility.team.msTeamId,
      id: LookupHelper.createTestGuid(),
      recorders: recorders.map(r => r.email),
      holdingCalls: []
    }

    console.log(`Creating test calendar ${calendar.calendarName}...`);

    // Add focus user(s) to team and channels, if required
    if (options?.addUsersToTeam && options?.focusUsers !== undefined && options?.focusUsers.length > 0) {
      const graphHelper = new GraphHelper();
      await graphHelper.TeamMembersCreateMultiple(facility.team.msTeamId, focusUsers);

      const promises: Promise<AadConversationMember>[] = [];
      options.focusUsers.forEach(async user => {
        // Get list of private channels this user has access to
        const settingsChannels = (await TenantSettingsHelper.getChannels()).filter(c => c.membershipType === "private" && c.accessLevels.indexOf(user.accessLevel ?? "tier0") > -1);

        // Loop through facility channels, if matched with above list the user has access
        facility.team.channels.forEach(facilityChannel => {
          if (settingsChannels.map(c => c.name).indexOf(facilityChannel.name) > -1) {
            promises.push(graphHelper.TeamChannelMemberCreate(facility.team.msTeamId, facilityChannel.msChannelId, user.msAadId!));
          }
        });
      });

      // Add users to channels
      if (promises.length > 0) {
        const response = await Promise.all(promises);
        response.forEach(user => {
          console.log(`Added user '${user.email}' (id: ${user.id}) to a channel in team '${facility.team.msTeamId}'`);
        });
      }
    }

    // Create the database object
    await this._database.container("Calendars").items.create(calendar);
    return calendar;
  }

  async createTestHoldingCall(options: { calendar: ICalendar, createInPast?: boolean, createMeeting?: boolean }): Promise<IHoldingCall> {
    const now = moment();
    let month = now;
    if (!options.createInPast)
      month = month.add(1, "month");
    else
      month = month.subtract(1, "month");
    const startTime = moment(month).toDate();
    const endTime = month.add(1, "hour").toDate();

    // Create online meeting via Graph API
    let graphOnlineMeeting: OnlineMeeting | undefined;
    if (options.createMeeting) {
      const graphHelper = new GraphHelper();
      graphOnlineMeeting = await graphHelper.OnlineMeetingCreateOrGet(GraphAccessUserType.WaitingRoomService, LookupHelper.createTestGuid(), startTime, endTime, `__test_Holding Call for ${options.calendar.externalCalendarId}`);
    }

    const holdingCall: IHoldingCall = {
      msMeetingId: graphOnlineMeeting ? graphOnlineMeeting.id : this.createMsMeetingId(),
      msThreadId: graphOnlineMeeting ? graphOnlineMeeting.chatInfo.threadId : this.createMsThreadId(),
      startTime: startTime.toISOString(),
      endTime: endTime.toISOString(),
      isExpired: false,
      msJoinInfo: this.createMsJoinInfo(graphOnlineMeeting)
    };

    console.log(`Creating test holding call ${holdingCall.msMeetingId} in calendar ${options.calendar.calendarName}...`);
    if (!options.calendar.holdingCalls) options.calendar.holdingCalls = [];
    options.calendar.holdingCalls.forEach(cal => {
      cal.isExpired = true;
    });
    options.calendar.holdingCalls.push(holdingCall);
    await this._database.container("Calendars").items.upsert(options.calendar);
    return holdingCall;
  }

  private createMsMeetingId(): string {
    return [...Array(124)].map(i => (~~(Math.random() * 36)).toString(36)).join("");
  }

  private createMsThreadId(): string {
    return `19:meeting_${LookupHelper.createTestGuid()}@thread.v2`;
  }

  private createMsJoinInfo(meeting?: OnlineMeeting): IMsJoinInfo {
    return {
      telephone: {
        additionalInformation: meeting !== undefined ? meeting.audioConferencing.dialInUrl : `https://dialin.teams.microsoft.com/${LookupHelper.createTestGuid()}?id=999471340`,
        conferenceId: meeting !== undefined ? meeting.audioConferencing.conferenceId : parseInt((Math.random() * 999999999).toFixed(0)),
        fullNumber: meeting !== undefined ? `${meeting.audioConferencing.tollNumber},,${meeting.audioConferencing.conferenceId}#` : "+1 872-240-8002,,266822956#",
        phoneNumber: meeting !== undefined ? meeting.audioConferencing.tollFreeNumber : "+1 872-240-8002"
      },
      url: meeting !== undefined ? meeting.joinWebUrl : `https://teams.microsoft.com/l/meetup-join/${this.createMsThreadId()}/1594305475269?context=%7b%22Tid%22%3a%22${LookupHelper.createTestGuid()}%22%2c%22Oid%22%3a%22${LookupHelper.createTestGuid()}%22%7d`
    }
  }

  async createTestRecorder(options?: { department?: ILookupItem, createMsUser?: boolean, isInactive?: boolean }): Promise<IRecorder> {
    const department = options?.department ?? await this.createTestLookup("Departments", "department");
    const id = LookupHelper.createTestGuid();

    const recorder: IRecorder = {
      id,
      email: LookupHelper.createTestDomainEmail(),
      accessLevel: "tier2",
      activeFlag: !(options?.isInactive ?? false),
      departmentId: department.id,
      departmentName: department.name,
      displayName: `__test_Test Recorder ${id}`,
      locationName: "location1",
      msAadId: LookupHelper.createTestGuid(),
      provisioningStatus: "Provisioned",
      recordingTypeId: "2",
      recordingTypeName: "Audio+Video",
      streamTypeId: "2",
      streamTypeName: "Audio",
      status: "Active"
    };

    // Create the recorder in AAD, if required
    if (options?.createMsUser) {
      const graphHelper = new GraphHelper();
      const msUser = await graphHelper.UserCreateFromRecorder(recorder);
      if (msUser !== undefined) recorder.msAadId = msUser.id;
    }

    console.log(`Creating test recorder ${recorder.displayName}...`);
    await this._database.container("Recorders").items.create(recorder);
    return recorder;
  }

  async createTestOnDemandMeeting(facility?: IFacility): Promise<IOnDemandMeeting> {
    if (!facility) facility = await this.createTestFacility();

    const id = LookupHelper.createTestGuid();
    const msMeetingId = LookupHelper.createTestGuid();
    const meetingName = `__test_Test on-demand meeting ${id}`;
    const now = moment();
    let month = now;
    month = month.add(1, "month");
    const startTime = moment(month).toDate();
    const endTime = month.add(1, "hour").toDate();

    let meeting: OnlineMeeting | undefined;
    if (!facility.team.msTeamId.startsWith("7e57")) {
      const graphHelper = new GraphHelper();
      meeting = await graphHelper.OnlineMeetingCreateOrGet(GraphAccessUserType.OnDemandService, msMeetingId, startTime, endTime, meetingName);
    }

    const onDemandMeeting: IOnDemandMeeting = {
      id,
      activeFlag: true,
      audioConferencing: meeting === undefined ? undefined : meeting.audioConferencing,
      endDateTime: endTime.toISOString(),
      facilityId: facility.id,
      joinUrl: meeting === undefined ? "http://join.here/" : meeting.joinWebUrl,
      meetingName,
      msMeetingId: meeting === undefined ? msMeetingId : meeting.id,
      msTeamId: facility.team.msTeamId,
      msThreadId: meeting === undefined ? LookupHelper.createTestGuid() : meeting.chatInfo.threadId,
      startDateTime: startTime.toISOString(),
      organizer: LookupHelper.createTestEmail(),
    };
    console.log(`Creating test on-demand meeting ${onDemandMeeting.meetingName}...`);
    await this._database.container("OnDemandMeetings").items.create(onDemandMeeting);
    return onDemandMeeting;
  }

  async getOnDemandMeeting(teamId: string): Promise<IOnDemandMeeting> {
    const result = await this.executeQuery(
      "OnDemandMeetings",
      `SELECT * FROM c WHERE c.msTeamId = '${teamId}'`,
      teamId
    );
    return isEmpty(result) ? undefined : result[0];
  }

  async listOnDemandMeetings(): Promise<IOnDemandMeeting[]> {
    return this.executeQuery("OnDemandMeetings", "SELECT * FROM c");
  }

  async createTestUser(options?: { department?: ILookupItem, role?: ILookupItem, title?: ILookupItem, createMsUser?: boolean, isInactive?: boolean }): Promise<IUser> {
    // Create the lookup objects
    const title = options?.title ?? await this.createTestLookup("Titles", "title");
    const role = options?.role ?? await this.createTestLookup("PersonnelRoles", "role");
    const dept = options?.department ?? await this.createTestLookup("Departments", "department");

    // Create the user object
    const id = LookupHelper.createTestGuid();
    const user: IUser = {
      id,
      msAadId: LookupHelper.createTestGuid(),
      email: LookupHelper.createTestEmail(),
      displayName: `__test_Test User ${id}`,
      firstName: "__test_Test",
      lastName: "__test_User",
      titleId: title.id,
      titleName: title.name,
      roleId: role.id,
      roleName: role.name,
      departmentId: dept.id,
      departmentName: dept.name,
      phone: "123-123-123-123",
      accessLevel: "tier2",
      inviteRedeemUrl: "",
      activeFlag: !(options?.isInactive ?? false),
      usageLocation: "US"
    };
    console.log(`Creating test user ${user.displayName}...`);

    // Create the user in AAD, if required
    if (options?.createMsUser) {
      const invite = await this.createMsUser(user);
      user.msAadId = invite.invitedUser.id;
      user.inviteRedeemUrl = invite.inviteRedeemUrl;
      user.usageLocation = await TenantSettingsHelper.getDefaultUsageLocation();
    }

    // Create the database object
    await this._database.container("Users").items.create(user);
    return user;
  }

  private async createMsUser(user: IUser): Promise<Invitation> {
    // Create user invitation
    const keyvaultHelper = new KeyvaultHelper();
    const everyoneTeamId = await keyvaultHelper.GetEveryoneTeamId();
    const generalChannelId = await keyvaultHelper.GetInfoTeamGeneralChannelId();
    const tenantId = await TenantSettingsHelper.getTenantId();
    const invitationRedirectUrl = `https://teams.microsoft.com/l/channel/${generalChannelId}/General?groupId=${everyoneTeamId}&tenantId=${tenantId}`;
    const graphHelper = new GraphHelper();
    const invitation = await graphHelper.UserInvitationsCreate(user.email, invitationRedirectUrl);

    // Update AAD user
    user.usageLocation = await TenantSettingsHelper.getDefaultUsageLocation();
    await graphHelper.UserUpdate(invitation.invitedUser.id, user);

    // Add user to the Everyone team
    const member = await graphHelper.TeamMembersCreate(everyoneTeamId, invitation.invitedUser.id);
    console.log(`Added user '${member.displayName}' (id: ${member.userId}) to team '${everyoneTeamId}'`);

    return invitation;
  }

  async updateUser(user: IUser): Promise<void> {
    console.log(`Updating user ${user.displayName}...`);
    await this._database.container("Users").items.upsert(user);
  }

  async getUser(id: string, email: string): Promise<IUser | undefined> {
    const list = await this.executeQuery("Users", `SELECT * FROM u WHERE u.id = '${id}'`, email);
    return list.length === 0 ? undefined : list[0];
  }

  async listUsers(testOnly = false): Promise<IUser[]> {
    let sql = "SELECT * FROM u";
    if (testOnly) sql += ` WHERE STARTSWITH(u.email, "__test_", true)`;
    return this.executeQuery("Users", sql);
  }

  async createTestLookup(containerName: string, lookupType: string, isDeleted = false): Promise<ILookupItem> {
    const id = LookupHelper.createTestGuid();
    const lookup = {
      id,
      name: `test ${lookupType} ${id}`,
      status: isDeleted ? "Deleted" : "Active"
    }
    console.log(`Creating test ${lookupType} ${id}...`);
    await this._database.container(containerName).items.create(lookup);
    return lookup;
  }

  async createTestCountry() {
    const countryId = LookupHelper.createTestGuid();
    const country = {
      id: countryId,
      name: `test country ${countryId}`,
      status: "Active",
      type: "country"
    };
    console.log(`Creating ${country.name}...`);
    return await this._database.container("Locations").items.create(country);
  }

  async createTestBuilding(): Promise<IBuilding> {
    const countryId = LookupHelper.createTestGuid();
    const country = {
      id: countryId,
      name: `test country ${countryId}`,
      status: "Active",
      type: "country"
    };
    console.log(`Creating ${country.name}...`);
    await this._database.container("Locations").items.create(country);

    const stateId = LookupHelper.createTestGuid();
    const state = {
      id: stateId,
      name: `test state ${stateId}`,
      status: "Active",
      type: "state",
      countryId,
      countryName: country.name,
    };
    console.log(`Creating ${state.name}...`);
    await this._database.container("Locations").items.create(state);

    const regionId = LookupHelper.createTestGuid();
    const region = {
      id: regionId,
      name: `test region ${regionId}`,
      nameAbbreviated: "tr",
      status: "Active",
      type: "region",
      countryId,
      countryName: country.name,
      stateId,
      stateName: state.name,
    };
    console.log(`Creating ${region.name}...`);
    await this._database.container("Locations").items.create(region);

    const subRegionId = LookupHelper.createTestGuid();
    const subRegion = {
      id: subRegionId,
      name: `test subregion ${subRegionId}`,
      nameAbbreviated: "tsr",
      status: "Active",
      type: "subregion",
      countryId,
      countryName: country.name,
      stateId,
      stateName: state.name,
      regionId,
      regionName: region.name,
      regionAbbreviated: region.nameAbbreviated
    };
    console.log(`Creating ${subRegion.name}...`);
    await this._database.container("Locations").items.create(subRegion);

    const buildingId = LookupHelper.createTestGuid();
    const building = {
      id: buildingId,
      name: `test building ${buildingId}`,
      nameAbbreviated: "tb",
      status: "Active",
      type: "building",
      countryId,
      countryName: country.name,
      stateId,
      stateName: state.name,
      regionId,
      regionName: region.name,
      regionAbbreviated: region.nameAbbreviated,
      subRegionId,
      subRegionName: subRegion.name,
      subRegionAbbreviated: subRegion.nameAbbreviated
    };
    console.log(`Creating ${building.name}...`);
    await this._database.container("Locations").items.create(building);

    return building;
  }

  async createTestEvent(options?: { calendar?: ICalendar, createMeeting?: boolean, externalId?: string }): Promise<IEvent> {
    const calendar = options?.calendar ?? await this.createTestCalendar();

    const id = LookupHelper.createTestGuid();
    const eventNo = (Math.random() * 999999).toFixed(0).padStart(6, "0");
    const now = moment();
    let month = now;
    month = month.add(1, "month");
    const startTime = moment(month).toDate();
    const endTime = month.add(1, "hour").toDate();

    const caseId = `CASE${eventNo}`;
    const caseTitle = "Spy vs Spy";
    const subject = `${caseId} - ${caseTitle}`;

    let meeting: OnlineMeeting | undefined;
    if (options?.createMeeting) {
      const graphHelper = new GraphHelper();
      meeting = await graphHelper.OnlineMeetingCreateOrGet(GraphAccessUserType.ScheduledEventService, id, startTime, endTime, subject);
    }

    const event: IEvent = {
      body: "email body",
      calendarId: calendar.id,
      caseId,
      caseTitle,
      caseType: "federal",
      endTime: endTime.toISOString(),
      eventType: "virtual",
      externalCalendarId: calendar.externalCalendarId,
      externalId: options?.externalId ?? `event${eventNo}`,
      facilityId: calendar.facilityId!,
      id,
      msJoinInfo: meeting !== undefined ? this.createMsJoinInfo(meeting) : undefined,
      msMeetingId: meeting !== undefined ? meeting.id : LookupHelper.createTestGuid(),
      msThreadId: meeting !== undefined ? meeting.chatInfo.threadId : LookupHelper.createTestGuid(),
      optionalAttendees: ["jtingey-labs@fortherecord.com"],
      requiredAttendees: ["jtingey@fortherecord.com"],
      startTime: startTime.toISOString(),
      status: "Active",
      subject,
      msTeamId: calendar.msTeamId
    };
    console.log(`Creating test event ${event.externalId}...`);
    await this._database.container("Events").items.create(event);
    return event;
  }

  async updateEvent(event: IEvent): Promise<void> {
    console.log(`Updating event ${event.subject}...`);
    await this._database.container("Events").items.upsert(event);
  }

  async listEvents(): Promise<IEvent[]> {
    return this.executeQuery(
      "Events",
      "SELECT * FROM c"
    );
  }

  // Finds any data created by the test suite and deletes it, optionally restricted to a subset of containers
  async deleteTestData(verbose = false, containers?: string[]): Promise<void> {
    try {
      const promises: Promise<OperationResponse[] | undefined>[] = [];
      if (containers === undefined || containers.includes("Locations")) promises.push(...await this.deleteTestDataInContainer("Locations", verbose, "id", "id"));
      if (containers === undefined || containers.includes("Titles")) promises.push(...await this.deleteTestDataInContainer("Titles", verbose, "id", "id"));
      if (containers === undefined || containers.includes("Departments")) promises.push(...await this.deleteTestDataInContainer("Departments", verbose, "id", "id"));
      if (containers === undefined || containers.includes("PersonnelRoles")) promises.push(...await this.deleteTestDataInContainer("PersonnelRoles", verbose, "id", "id"));
      if (containers === undefined || containers.includes("Facilities")) promises.push(...await this.deleteTestDataInContainer("Facilities", verbose, "id", "id"));
      if (containers === undefined || containers.includes("Calendars")) promises.push(...await this.deleteTestDataInContainerWithQuery("Calendars", verbose, "SELECT c.id, c.id as pk FROM c WHERE c.id LIKE '7e57%' OR c.calendarName LIKE '__test_Test%'"));
      if (containers === undefined || containers.includes("Recorders")) promises.push(...await this.deleteTestDataInContainerWithQuery("Recorders", verbose, "SELECT c.id, c.id as pk FROM c WHERE c.displayName LIKE '__test_Test Recorder%'"));
      if (containers === undefined || containers.includes("Users")) promises.push(...await this.deleteTestDataInContainerWithQuery("Users", verbose, "SELECT c.id, c.email as pk FROM c WHERE c.id LIKE '7e57%' OR c.email LIKE '__test_%'"));
      if (containers === undefined || containers.includes("Events")) promises.push(...await this.deleteTestDataInContainerWithQuery("Events", verbose, "SELECT c.id, c.calendarId as pk FROM c WHERE c.id LIKE '7e57%' OR c.subject LIKE '__test%'"));
      if (containers === undefined || containers.includes("OnDemandMeetings")) promises.push(...await this.deleteTestDataInContainerWithQuery("OnDemandMeetings", verbose, "SELECT c.id, c.msTeamId as pk FROM c WHERE c.id LIKE '7e57%' OR c.meetingName LIKE '__test%'"));
      const responses = <OperationResponse[][]>((await Promise.all(promises)).filter(r => r !== undefined));
      const successCount = responses.flat().filter(r => r.statusCode >= 200 && r.statusCode < 300).length;
      console.log(`Deleted ${successCount} test documents.`);
      const rateLimitedCount = responses.flat().filter(r => r.statusCode === 429).length;
      if (rateLimitedCount > 0) console.warn(`Failed to delete ${rateLimitedCount} documents because of Cosmos rate limiting.`);
    }
    catch (ex) {
      console.log("Error in deleteTestData()");
      console.error(ex);
    }
  }

  // Returns an array of promises to bulk delete data in the specifed container, using the provided id and partition key names to construct the select query
  private deleteTestDataInContainer(containerName: string, verbose: boolean, idName: string, partitionKeyName: string): Promise<Promise<OperationResponse[]>[]> {
    return this.deleteTestDataInContainerWithQuery(containerName, verbose, `SELECT c.${idName} as id, c.${partitionKeyName} as pk FROM c WHERE c.${idName} LIKE '7e57%'`);
  }

  // Returns an array of promises to bulk delete data in the specifed container, with the provided query specifying which documents to delete
  private async deleteTestDataInContainerWithQuery(containerName: string, verbose: boolean, query: string): Promise<Promise<OperationResponse[]>[]> {
    if (verbose) console.log(`Looking for test data in container '${containerName}'...`);
    const itemsToDelete: { id: string, pk: string }[] = await this.executeQuery(containerName, query);
    if (itemsToDelete.length === 0) {
      if (verbose) console.log("No test data found.");
      return [];
    }
    else {
      if (verbose) console.log(`Found ${itemsToDelete.length} items, deleting...`);
      const operations: OperationInput[] = itemsToDelete.map(item => { return { operationType: "Delete", id: item.id, partitionKey: item.pk } });
      const splitOperations = this.sliceIntoChunks(operations, 75);
      return splitOperations.map(operations => this._database.container(containerName).items.bulk(operations, { continueOnError: true }));
    }
  }

  // Splits an array into multiple chunks of the specified size
  private sliceIntoChunks<T>(arr: T[], chunkSize: number): T[][] {
    const result: T[][] = [];
    if (arr !== undefined && arr.length > 0) {
      for (let i = 0; i < arr.length; i += chunkSize) {
        const chunk = arr.slice(i, i + chunkSize);
        result.push(chunk);
      }
    }
    return result;
  }

  private async executeQuery(containerId: string, query: string, partitionKey?: string): Promise<any> {
    const queryIterator = this._database.container(containerId).items
      .query(query, {
        maxItemCount: 1000,
        maxDegreeOfParallelism: 100,
        bufferItems: true,
        partitionKey
      });

    const data = new Array<any>();

    while (queryIterator.hasMoreResults()) {
      const { resources: results } = await queryIterator.fetchNext();
      if (results) {
        data.push(...results);
      }
    }

    return data;
  }

  public async deleteOnDemandMeetings(meetingsToDelete: IOnDemandMeeting[]): Promise<any> {
    if (meetingsToDelete.length === 0) return;
    const operations: OperationInput[] = meetingsToDelete.map(item => { return { operationType: "Delete", id: item.id, partitionKey: item.msTeamId } });
    const splitOperations = this.sliceIntoChunks(operations, 75);
    return splitOperations.map(operations => this._database.container("OnDemandMeetings").items.bulk(operations, { continueOnError: true }));
  }

  public async deleteEvents(eventsToDelete: IEvent[]): Promise<any> {
    if (eventsToDelete.length === 0) return;
    const operations: OperationInput[] = eventsToDelete.map(item => { return { operationType: "Delete", id: item.id, partitionKey: item.calendarId } });
    const splitOperations = this.sliceIntoChunks(operations, 75);
    return splitOperations.map(operations => this._database.container("Events").items.bulk(operations, { continueOnError: true }));
  }

  public async deleteCalendars(calendarsToDelete: ICalendar[]): Promise<any> {
    if (calendarsToDelete.length === 0) return;
    const operations: OperationInput[] = calendarsToDelete.map(item => { return { operationType: "Delete", id: item.id, partitionKey: item.id } });
    const splitOperations = this.sliceIntoChunks(operations, 75);
    return splitOperations.map(operations => this._database.container("Calendars").items.bulk(operations, { continueOnError: true }));
  }
}

export interface IFacility {
  buildingId: string;
  buildingName: string,
  buildingAbbreviated: string;
  countryId: string;
  countryName: string;
  displayName: string;
  facilityType: string;
  floor: string;
  id: string;
  regionId: string;
  regionName: string,
  regionAbbreviated: string;
  room: string;
  stateId: string;
  stateName: string,
  subRegionId: string;
  subRegionAbbreviated: string;
  subRegionName: string,
  team: IFacilityTeam;
  calendars?: { id: string, externalCalendarId: string }[];
  activeOnDemandMeetingCount?: number;
}

export interface IFacilityTeam {
  channels: Array<IFacilityChannel>;
  apps: Array<IFacilityApp>;
  msTeamId: string;
  name: string;
  type: string;
}

export interface IFacilityChannel {
  addRecorderUser: boolean;
  isDefaultChannel: boolean;
  msChannelId: string;
  name: string;
}

export interface IFacilityApp {
  msChannelId: string;
  msTeamsAppId: string;
  msTeamsAppInstallationId: string;
}

export interface IFacilityCreateUpdateParams {
  buildingId: string;
  displayName: string;
  facilityType: string;
  floor: string;
  id: string;
  room: string;
}

export interface ICalendarCreateUpdateParams {
  calendarName?: string;
  focusUsers?: string[];
  departmentId?: string;
  externalCalendarId: string;
  facilityId?: string;
  recorders?: string[];
  holdingCalls?: IHoldingCall[];
}

export interface ICalendar extends ICalendarCreateUpdateParams {
  departmentName: string;
  holdingCalls?: IHoldingCall[];
  id: string;
  msTeamId?: string;
}

export interface ICalendarSummary {
  calendarName: string;
  focusUsers: string[];
  externalCalendarId: string;
  facilityId: string;
  facilityName: string;
  id: string;
  personnelCount: number;
  recorderCount: number;
}

export interface IHoldingCall {
  msMeetingId: string;
  msThreadId: string;
  startTime: string;
  endTime: string;
  isExpired: boolean;
  msJoinInfo: IMsJoinInfo;
}

export interface IHoldingCallCreateParams {
  externalCalendarId: string;
  startTime: string;
  endTime: string;
}

export interface ICalendarHoldingCall {
  externalCalendarId: string;
  calendarName: string;
  holdingCall: IHoldingCall;
}

export interface ICalendarHoldingCalls {
  externalCalendarId: string;
  calendarName: string;
  holdingCalls: IHoldingCall[];
}

export interface ISetting {
  id: string;
  tenantId: string,
  defaultUsageLocation: string,
  channels: Array<ISettingChannel>,
  accessLevelDescriptions: Array<ISettingAccessLevel>,
  meetingsChannelApps: Array<IMeetingsChannelApp>
}

export interface ISettingChannel {
  name: string;
  isDefaultChannel: boolean;
  addRecorderUser: boolean;
  type: string;
  membershipType: string;
  accessLevels: Array<string>;
}

export interface IMeetingsChannelApp {
  teamsAppId: string;
  displayName: string;
  contentUrl: string;
}

export interface ISettingAccessLevel {
  accessLevel: string;
  description: string;
}

export interface IRecorder {
  accessLevel: string;
  activeFlag: boolean;
  departmentId: string;
  departmentName: string,
  displayName: string;
  email: string;
  id: string;
  locationName: string;
  msAadId?: string;
  provisioningStatus: string;
  recordingTypeId: string;
  recordingTypeName: string,
  streamTypeId: string;
  streamTypeName: string;
  status: string;
}

export interface ILookupItem {
  id: string,
  name: string,
  status?: string,
  activeRelations?: { name: string, activeCount: number }[]
}

export interface ILocation extends ILookupItem {
  type?: string;
}

export interface IState extends ILocation {
  countryId: string;
  countryName?: string;
}

export interface IRegion extends IState {
  nameAbbreviated?: string;
  stateId: string;
  stateName?: string;
}

export interface ISubRegion extends IRegion {
  regionId: string;
  regionName?: string;
}

export interface IBuilding extends ISubRegion {
  subRegionId: string;
  subRegionName?: string;
}

export interface IOnDemandMeetingCreateParams {
  endDateTime?: string;
  meetingName: string;
  organizer?: string;
  startDateTime?: string;
  teamId: string;
}

export interface IOnDemandMeeting {
  id: string;
  activeFlag: boolean;
  audioConferencing?: IAudioConferencing;
  endDateTime: string;
  facilityId: string;
  joinUrl: string;
  meetingName: string;
  msMeetingId: string;
  msTeamId: string;
  msThreadId: string;
  organizer: string;
  startDateTime: string;
}

export interface IChatMessageCreateParams {
  teamId: string;
  meetingId: string;
  content: string;
}

export interface IChatMessage {
  body: IChatMessageBody;
  chatId: string;
  createdDateTime: string;
  from: any;
  id: string;
  messageType: string;
  subject: string;
}

export interface IChatMessageBody {
  content: string;
  contentType: string;
}

export interface IAudioConferencing {
  conferenceId?: number;
  tollNumber: string;
  tollFreeNumber: string;
  dialInUrl: string;
  tollNumbers: string[];
  tollFreeNumbers: string[];
}

export interface IEventCreateUpdateParams {
  body?: string;
  caseId?: string;
  caseTitle?: string;
  caseType?: string;
  endTime: string;
  eventType?: string;
  externalCalendarId: string;
  optionalAttendees?: string[];
  partyRegistrationUrl?: string;
  requiredAttendees?: string[];
  startTime: string;
  subject?: string;
}

export interface IEvent extends IEventCreateUpdateParams {
  id: string;
  calendarId: string;
  externalId: string;
  facilityId: string;
  msTeamId?: string;
  msJoinInfo?: IMsJoinInfo;
  msMeetingId?: string;
  msThreadId?: string;
  status: string;
}

export interface IEventConversationMembersResponse {
  eventId: string;
  members: ConversationMember[]
}

export interface IMeetingConversationMembersResponse {
  msMeetingId: string;
  externalCalendarId: string;
  members: ConversationMember[]
}

export interface IMsJoinInfo {
  telephone: ITelephone;
  url: string;
}

export interface ITelephone {
  additionalInformation?: string;
  conferenceId?: number;
  fullNumber?: string;
  phoneNumber?: string;
}

export interface IUser extends IUserCreateUpdateParams {
  activeFlag: boolean;
  departmentName: string;
  id: string;
  msAadId?: string;
  roleName: string;
  titleName: string;
  usageLocation: string;
}

export interface IUserCreateUpdateParams {
  accessLevel: string;
  departmentId: string;
  displayName: string;
  email: string;
  firstName: string;
  inviteRedeemUrl?: string;
  lastName: string;
  phone?: string;
  roleId: string;
  titleId?: string;
}