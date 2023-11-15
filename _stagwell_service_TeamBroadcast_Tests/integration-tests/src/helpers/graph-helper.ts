import Axios, { AxiosRequestConfig, AxiosResponse } from "axios";
import { expect } from "chai";
import { CosmosDbHelper, ICalendar, IChatMessage, IRecorder, ISettingChannel, IUser } from "./cosmos-helper";
import { KeyvaultHelper } from "./keyvault-helper";

export class GraphHelper {
    private static AccessTokens: { [userType: number]: string } = {};
    private static AccessTokenPromises: { [userType: number]: Promise<string> } = {};

    // Creates a new Team
    public async TeamsCreate(displayName: string, teamType: string): Promise<string> {
        console.log(`Creating new team '${displayName}'...`);

        // Create request object
        const team = {
            "template@odata.bind": "https://graph.microsoft.com/v1.0/teamsTemplates('standard')",
            displayName,
            description: `${teamType} team for ${displayName}`,
            visibility: "Private"
        };

        // Send request to Graph API
        const response = await this.GraphPost(GraphAccessUserType.ScheduledEventService, "/teams", team);
        if (response.status !== 202) throw new Error("Failed to create team: " + response.data);

        // Wait for completion of team create operation
        const completion = await this.GetTeamsOperationCompletion(response.headers["location"]);
        if (completion !== undefined && completion.status === "succeeded") {
            console.log(`Team was successfully created with id ${completion.targetResourceId}.`);
            return completion.targetResourceId;
        }
        else {
            if (completion === undefined)
                throw new Error(`Team was not successfully created, there was an unknown error.`);
            else
                throw new Error(`Team was not successfully created, status ${completion.status}, error code '${completion.error.code}', error message: ${completion.error.message}.`);
        }
    }

    // Gets the Teams async operation status at the provided URI and waits for progress to complete, or timeout
    private async GetTeamsOperationCompletion(uri: string): Promise<TeamsAsyncOperation | undefined> {
        let operation: TeamsAsyncOperation | undefined;

        // Poll for completion of operation up to 20 times
        for (let i = 0; i < 20; i++) {
            await new Promise(resolve => setTimeout(resolve, 10000)); // wait 10s between tries
            const response = await this.GraphGet(GraphAccessUserType.ScheduledEventService, uri);
            operation = response.data;
            if (operation!.status !== "inProgress" && operation!.status !== "notStarted") return operation;
        }

        // Timeout
        return operation;
    }

    // Gets a list of all teams, including id and display name only
    public async TeamsList(expectedStatus = 200): Promise<{ id: string, displayName: string }[]> {
        const response = await this.GraphGet(GraphAccessUserType.ScheduledEventService, "/groups", "$filter=resourceProvisioningOptions/any(x:x+eq+'Team')&$select=id,displayName");
        expect(response.status).equals(expectedStatus);

        if (response.status !== 200) {
            console.error(`Could not get list of teams: ${response.statusText}`);
            console.error(response.data);
            return [];
        }

        return response.data.value;
    }

    // get a team with the specfied id
    public async TeamsGet(msTeamId: string, expectedStatus = 200): Promise<any> {
        const response = await this.GraphGet(GraphAccessUserType.ScheduledEventService, `/teams/${msTeamId}`);
        expect(response.status).equals(expectedStatus);
        if (response.status !== 200) {
            console.log(`Could not find team with id '${msTeamId}'`);
            return undefined;
        }
        return response.data;
    }

    // Deletes a team with the specified id
    public async TeamsDelete(msTeamId: string, expectedStatus?: number): Promise<number> {
        const response = await this.GraphDelete(GraphAccessUserType.ScheduledEventService, `/groups/${msTeamId}`);
        if (expectedStatus !== undefined) expect(response.status).equals(expectedStatus);

        if (response.status !== 204 && response.status !== 404) {
            console.error(`Could not delete team '${msTeamId}': ${response.statusText}`);
            console.error(response.data);
        }

        return response.status;
    }

    public async TeamsInstalledAppsList(msTeamId: string): Promise<string[]> {
        const response = await this.GraphGet(GraphAccessUserType.ScheduledEventService, `/teams/${msTeamId}/installedApps`);
        expect(response.status).equals(200);
        return response.data.value.map(x => x.id);
    }

    public async TeamsInstalledAppGet(msTeamId: string, appId: string): Promise<any> {
        const response = await this.GraphGet(GraphAccessUserType.ScheduledEventService, `/teams/${msTeamId}/installedApps/${appId}`);
        expect(response.status).equals(200);
        return response.data.value;
    }

    public async TeamChannelCreate(msTeamId: string, channel: ISettingChannel): Promise<Channel | undefined> {
        console.log(`Creating new channel '${channel.name}' in team '${msTeamId}'...`);
        const req = {
            "@odata.type": "#microsoft.graph.channel",
            displayName: channel.name,
            membershipType: channel.membershipType
        };
        const response = await this.GraphPost(GraphAccessUserType.ScheduledEventService, `/teams/${msTeamId}/channels`, req);
        if (response.status !== 201) {
            console.error(`Could not create channel '${channel.name}' in team '${msTeamId}'`);
            console.error(response.data);
            return undefined;
        }
        else {
            return response.data;
        }
    }

    public async TeamChannelsCreate(msTeamId: string, channels: ISettingChannel[]): Promise<Channel[]> {
        console.log(`Creating new channels ${channels.map(c => c.name).join(", ")} in team '${msTeamId}'...`);
        const url = `/teams/${msTeamId}/channels`;
        const requests = channels.map(channel => {
            return {
                "@odata.type": "#microsoft.graph.channel",
                displayName: channel.name,
                membershipType: channel.membershipType
            }
        });
        requests.forEach(req => {
            if (req.membershipType === "standard") req["isFavoriteByDefault"] = true;
            else if (req.membershipType !== "private") req["isFavoriteByDefault"] = false;
        });
        const batchResponse = await this.GraphBatch(GraphAccessUserType.ScheduledEventService, "POST", requests.map(r => { return { url, body: r } }));
        expect(batchResponse.status).equals(200);

        batchResponse.data.responses.filter(r => r.status !== 201).forEach(r => {
            console.error(`Could not create channel in team ${msTeamId}: ${r.body}`);
        });
        return batchResponse.data.responses.filter(r => r.status === 201).map(r => r.body);
    }

    public async TeamChannelTabsList(teamId: string, channelId: string): Promise<TeamsTab[]> {
        const response = await this.GraphGet(GraphAccessUserType.ScheduledEventService, `/teams/${teamId}/channels/${channelId}/tabs`);
        if (response.status === 404) return [];
        expect(response.status).equals(200);
        return response.data.value;
    }

    public async TeamMembersList(msTeamId: string, expectedStatus: number = 200): Promise<TeamMember[]> {
        const response = await this.GraphGet(GraphAccessUserType.App, `/teams/${msTeamId}/members`);
        expect(response.status).equals(expectedStatus);
        return response.status === 200 ? response.data.value : [];
    }

    public async TeamMembersCreate(msTeamId: string, msAadId: string): Promise<AadConversationMember> {
        const request = {
            "@odata.type": "#microsoft.graph.aadUserConversationMember",
            roles: [],
            "user@odata.bind": `https://graph.microsoft.com/v1.0/users('${msAadId}')`
        };
        const response = await this.GraphPost(GraphAccessUserType.ScheduledEventService, `/teams/${msTeamId}/members`, request);
        expect(response.status).equals(201);
        return response.data;
    }

    public async TeamMembersCreateMultiple(msTeamId: string, users: IUser[]): Promise<ConversationMember[]> {
        const members: ConversationMember[] = [];
        for (let user of users) {
            const request = {
                "@odata.type": "#microsoft.graph.aadUserConversationMember",
                roles: [],
                "user@odata.bind": `https://graph.microsoft.com/v1.0/users('${user.msAadId}')`
            };
            const response = await this.GraphPost(GraphAccessUserType.ScheduledEventService, `/teams/${msTeamId}/members`, request);
            expect(response.status).equals(201);
            members.push(response.data);
        }
        return members;
    }

    public async TeamChannelMembersList(msTeamId: string, msChannelId: string): Promise<AadConversationMember[]> {
        const response = await this.GraphGet(GraphAccessUserType.App, `/teams/${msTeamId}/channels/${msChannelId}/members`);
        expect(response.status).equals(200);
        return response.data.value;
    }

    public async TeamChannelMemberCreate(msTeamId: string, msChannelId: string, userId: string): Promise<AadConversationMember> {
        const request = {
            "@odata.type": "#microsoft.graph.aadUserConversationMember",
            roles: [],
            "user@odata.bind": `https://graph.microsoft.com/v1.0/users('${userId}')`
        };
        const response = await this.GraphPost(GraphAccessUserType.App, `/teams/${msTeamId}/channels/${msChannelId}/members`, request);
        expect(response.status).equals(201);
        return response.data;
    }

    // Creates a new online meeting
    public async OnlineMeetingCreateOrGet(userType: GraphAccessUserType, externalId: string, startTime: Date, endTime: Date, subject: string, expectedStatus = 201): Promise<OnlineMeeting> {
        console.log(`Creating new meeting '${subject}'...`);

        // Create request object
        const meeting = {
            externalId,
            subject,
            startDateTime: startTime.toISOString(),
            endDateTime: endTime.toISOString()
        };

        // Send request to Graph API
        const response = await this.GraphPost(userType, "/me/onlineMeetings/createOrGet", meeting);
        expect(response.status).equals(expectedStatus);

        if (response.status === 201)
            console.log(`Meeting was successfully created with id ${response.data.id}.`);
        else
            console.log("Failed to create online meeting.");
        return response.data;
    }

    // Retrieves an existing online meeting
    public async OnlineMeetingGet(userType: GraphAccessUserType, meetingId: string, expectedStatus = 200): Promise<OnlineMeeting | undefined> {
        const response = await this.GraphGet(userType, `/me/onlineMeetings/${meetingId}`);
        expect(response.status).equals(expectedStatus);
        if (response.status !== 200) {
            console.log(`Could not find online meeting with id '${meetingId}'`);
            return undefined;
        }
        return response.data;
    }

    // Deletes an online meeting with the specified id
    public async OnlineMeetingDelete(userType: GraphAccessUserType, msMeetingId: string, expectedStatus?: number): Promise<{ msMeetingId: string, statusCode: number }> {
        if (msMeetingId.startsWith("7e57")) return { msMeetingId, statusCode: 404 };
        const response = await this.GraphDelete(userType, `/me/onlineMeetings/${msMeetingId}`);
        if (expectedStatus !== undefined) expect(response.status).equals(expectedStatus);

        if (response.status !== 204 && response.status !== 404) {
            console.error(`Could not delete online meeting '${msMeetingId}': ${response.statusText}`);
            console.error(response.data);
        }

        return { msMeetingId, statusCode: response.status };
    }

    public async ChatMessageGet(userType: GraphAccessUserType, chatId: string, messageId: string, expectedStatus = 200): Promise<IChatMessage | undefined> {
        const response = await this.GraphGet(userType, `/me/chats/${chatId}/messages/${messageId}`);
        expect(response.status).equals(expectedStatus);
        if (response.status !== 200) {
            console.log(`Could not find chat message with id '${messageId}'`);
            return undefined;
        }
        return response.data;
    }

    public async UserInvitationsCreate(email: string, inviteRedirectUrl: string): Promise<Invitation> {
        const response = await this.GraphPost(GraphAccessUserType.App, "/invitations", {
            inviteRedirectUrl,
            invitedUserEmailAddress: email,
            sendInvitationMessage: false
        });
        if (response.status === 201) {
            return response.data;
        }
        else {
            throw new Error(`Could not send invitation to user '${email}': ${response.data}`);
        }
    }

    public async UserGet(msUserId: string, expectedStatus = 200): Promise<User | undefined> {
        const response = await this.GraphGet(GraphAccessUserType.App, `/users/${msUserId}`);
        expect(response.status).equals(expectedStatus);
        if (response.status !== 200) {
            console.log(`Could not find user with id '${msUserId}'`);
            return undefined;
        }
        return response.data;
    }

    public async UsersList(emailStartsWith?: string): Promise<User[]> {
        const url = emailStartsWith === undefined ? "/users" : `/users?$filter=startswith(mail,'${emailStartsWith}')`;
        const response = await this.GraphGet(GraphAccessUserType.App, url);
        return response.data.value;
    }

    public async UserCreateFromRecorder(recorder: IRecorder): Promise<User | undefined> {
        const keyvaultHelper = new KeyvaultHelper();
        const password = await keyvaultHelper.GetDefaultRecorderUserPassword();
        return await this.UserCreate(recorder.email, recorder.displayName, recorder.departmentName, "Automated focus Recorder", recorder.locationName, password);
    }

    public async UserCreate(email: string, displayName: string, department: string, jobTitle: string, locationName: string, password: string): Promise<User | undefined> {
        const request = {
            accountEnabled: true,
            department,
            displayName,
            jobTitle,
            mail: email,
            mailNickname: locationName,
            passwordPolicies: "DisablePasswordExpiration",
            passwordProfile: {
                forceChangePasswordNextSignIn: false,
                password
            },
            userPrincipalName: email
        };
        const response = await this.GraphPost(GraphAccessUserType.App, "/users", request);
        if (response.status != 201) {
            console.error(response.data);
            return undefined;
        }
        else {
            return response.data;
        }
    }

    public async UserUpdate(msUserId: string, user: IUser): Promise<boolean> {
        const updateRequest = {
            department: user.departmentName,
            givenName: user.firstName,
            surname: user.lastName,
            usageLocation: user.usageLocation
        };
        const response = await this.GraphPatch(GraphAccessUserType.App, `/users/${msUserId}`, updateRequest);
        if (response.status === 204) {
            return true;
        }
        else {
            console.error(`Could not update user '${msUserId}'`);
            throw new Error(response.data);
        }
    }

    public async UserDelete(msUserId: string): Promise<number> {
        const response = await this.GraphDelete(GraphAccessUserType.ScheduledEventService, `/users/${msUserId}`);
        if (response.status === 204) {
            return 204;
        }
        else if (response.status === 404) {
            console.warn(`Could not delete user '${msUserId}': not found`);
            return 404;
        }
        else {
            console.error(`Could not update user '${msUserId}': ${response.statusText}`);
            console.error(response.data);
            return response.status;
        }
    }

    // Sends a GET request to the Graph API
    private async GraphGet(userType: GraphAccessUserType, path: string, query?: string): Promise<AxiosResponse<any>> {
        let url = `https://graph.microsoft.com/v1.0${path}`;
        if (query !== undefined) url = `${url}?${query}`;
        return Axios.get(url, await this.GraphApiAuthConfig(userType));
    }

    // Sends a POST request to the Graph API
    private async GraphPost(userType: GraphAccessUserType, path: string, data: any): Promise<AxiosResponse<any>> {
        return Axios.post(`https://graph.microsoft.com/v1.0${path}`, data, await this.GraphApiAuthConfig(userType));
    }

    // Sends a PATCH request to the Graph API
    private async GraphPatch(userType: GraphAccessUserType, path: string, data: any): Promise<AxiosResponse<any>> {
        return Axios.patch(`https://graph.microsoft.com/v1.0${path}`, data, await this.GraphApiAuthConfig(userType));
    }

    // Sends a DELETE request to the Graph API
    private async GraphDelete(userType: GraphAccessUserType, path: string): Promise<AxiosResponse<any>> {
        return Axios.delete(`https://graph.microsoft.com/v1.0${path}`, await this.GraphApiAuthConfig(userType));
    }

    // Sends a batch request to the Graph API
    private async GraphBatch(userType: GraphAccessUserType, method: string, requests: { id?: number, url: string, body?: any }[]): Promise<AxiosResponse<{ responses: { id: string, status: number, body?: any }[] }>> {
        const batchRequests: { id: string, method: string, url: string, body: any, headers: any }[] = [];
        for (let i = 0; i < requests.length; i++) {
            batchRequests.push({
                id: (requests[i].id ?? (i + 1)).toString(),
                method,
                url: requests[i].url,
                body: requests[i].body,
                headers: requests[i].body === undefined ? undefined : { "Content-Type": "application/json" }
            })
        }
        return await this.GraphPost(userType, "/$batch", { requests: batchRequests });
    }

    // Gets an axios request config object including the oauth token
    private async GraphApiAuthConfig(userType: GraphAccessUserType): Promise<AxiosRequestConfig> {
        const config: AxiosRequestConfig = {};
        config.headers = { Authorization: `Bearer ${await this.GetAccessToken(userType)}` };
        config.validateStatus = (status) => (status >= 200 && status < 500);
        return config;
    }

    // Gets an oauth access token for use in Graph API calls
    private async GetAccessToken(userType: GraphAccessUserType): Promise<string> {
        const accessToken = GraphHelper.AccessTokens[userType];
        if (accessToken !== undefined) return accessToken;

        const promise = GraphHelper.AccessTokenPromises[userType];
        if (promise === undefined) GraphHelper.AccessTokenPromises[userType] = this.GetAccessTokenPromise(userType);
        const token = await GraphHelper.AccessTokenPromises[userType];
        delete GraphHelper.AccessTokenPromises[userType];
        return token;
    }

    private async GetAccessTokenPromise(userType: GraphAccessUserType): Promise<string> {
        // Get config values from keyvault
        const keyvaultHelper = new KeyvaultHelper();
        const graphServiceCreds = await keyvaultHelper.GetSecret("graph-service-creds");
        if (graphServiceCreds === undefined) throw new Error("Graph Service credentials missing");
        const creds = JSON.parse(graphServiceCreds.replace(/'/g, "\""));
        let username: string | undefined;
        let password: string | undefined;
        switch (userType) {
            case GraphAccessUserType.ScheduledEventService:
                username = await keyvaultHelper.GetSecret("scheduled-event-service-user");
                password = await keyvaultHelper.GetSecret("scheduled-event-service-password");
                break;
            case GraphAccessUserType.WaitingRoomService:
                username = await keyvaultHelper.GetSecret("waiting-room-service-user");
                password = await keyvaultHelper.GetSecret("waiting-room-service-password");
                break;
            case GraphAccessUserType.OnDemandService:
                username = await keyvaultHelper.GetSecret("on-demand-meeting-service-user");
                password = await keyvaultHelper.GetSecret("on-demand-meeting-service-password");
                break;
        }

        // Fetch the token
        console.log(`Attempting oauth login for ${GraphAccessUserType[userType]} Graph API access...`);
        const req = new URLSearchParams({
            "grant_type": password === undefined ? "client_credentials" : "password",
            "resource": creds.audience,
            "client_id": creds.clientId,
            "client_secret": creds.secret
        });
        if (username !== undefined) req.append("username", username);
        if (password !== undefined) req.append("password", password);
        const response = await Axios.post<any>(`https://login.microsoftonline.com/${creds.tenant}/oauth2/token`, req.toString());

        // Return retrieved token or throw an exception
        if (response.status === 200) {
            console.log("Login succeeded");
            GraphHelper.AccessTokens[userType] = response.data["access_token"];
            return GraphHelper.AccessTokens[userType];
        }
        else {
            console.log("Login failed");
            throw new Error(response.data);
        }
    }

    public async deleteTestData(verbose = false): Promise<void> {
        console.log(`Deleted ${await this.deleteTestHoldingCalls(verbose)} test holding calls.`);
        console.log(`Deleted ${await this.deleteTestOnlineMeetings(verbose)} test online meetings.`);
        console.log(`Deleted ${await this.deleteTestEvents(verbose)} test events.`);
        console.log(`Deleted ${await this.deleteTestTeams(verbose)} test teams.`);
        console.log(`Deleted ${await this.deleteTestUsers(verbose)} test users.`);
    }

    public async deleteTestUsers(verbose = false): Promise<number> {
        try {
            if (verbose) console.log(`Looking for test users in AAD...`);
            const testUsers = await this.UsersList("__test_");
            if (testUsers.length === 0) {
                if (verbose) console.log("No users found.");
                return 0;
            }
            if (verbose) console.log(`Found ${testUsers.length} users, deleting...`);

            const promises = testUsers.map(u => this.UserDelete(u.id));
            const statusCodes = await Promise.all(promises);
            return statusCodes.filter(s => s === 204).length;
        }
        catch (ex) {
            console.log("Error in deleteTestUsers()");
            console.error(ex);
            return 0;
        }
    }

    public async deleteTestTeams(verbose = false): Promise<number> {
        try {
            if (verbose) console.log(`Looking for test teams in Graph...`);
            const allTeams = await this.TeamsList();
            const testTeams = allTeams.filter(t => t.displayName.startsWith("Facility 7e57"));
            if (testTeams.length === 0) {
                if (verbose) console.log("No teams found.");
                return 0;
            }
            if (verbose) console.log(`Found ${testTeams.length} teams, deleting...`);

            const promises = testTeams.map(t => this.TeamsDelete(t.id));
            const statusCodes = await Promise.all(promises);
            return statusCodes.filter(s => s === 204).length;
        }
        catch (ex) {
            console.log("Error in deleteTestTeams()");
            console.error(ex);
            return 0;
        }
    }

    public async deleteTestOnlineMeetings(verbose = false): Promise<number> {
        try {
            if (verbose) console.log(`Looking for test online meetings in database...`);
            const dbHelper = new CosmosDbHelper();
            const allMeetings = await dbHelper.listOnDemandMeetings();
            const testMeetings = allMeetings.filter(t => t.meetingName.startsWith("__test") && t.msMeetingId !== undefined && t.msMeetingId!.indexOf("7e57") === -1);
            if (testMeetings.length === 0) {
                if (verbose) console.log("No meetings found.");
                return 0;
            }
            if (verbose) console.log(`Found ${testMeetings.length} meetings, deleting...`);

            const promises = testMeetings.map(m => this.OnlineMeetingDelete(GraphAccessUserType.ScheduledEventService, m.msMeetingId));
            const results = await Promise.all(promises);
            const success = results.filter(s => s.statusCode === 204);
            const deletedMeetings = allMeetings.filter(m => success.map(s => s.msMeetingId).includes(m.msMeetingId));
            dbHelper.deleteOnDemandMeetings(deletedMeetings);
            return success.length;
        }
        catch (ex) {
            console.log("Error in deleteTestOnlineMeetings()");
            console.error(ex);
            return 0;
        }
    }

    public async deleteTestEvents(verbose = false): Promise<number> {
        try {
            if (verbose) console.log(`Looking for test events in database...`);
            const dbHelper = new CosmosDbHelper();
            const allEvents = await dbHelper.listEvents();
            const testEvents = allEvents.filter(e => (e.subject ?? "").startsWith("__test") && e.msMeetingId !== undefined && e.msMeetingId!.indexOf("7e57") === -1);
            if (testEvents.length === 0) {
                if (verbose) console.log("No events found.");
                return 0;
            }
            if (verbose) console.log(`Found ${testEvents.length} events, deleting...`);

            const promises = testEvents.map(m => this.OnlineMeetingDelete(GraphAccessUserType.ScheduledEventService, m.msMeetingId!));
            const results = await Promise.all(promises);
            const success = results.filter(s => s.statusCode === 204);
            const deletedEvents = allEvents.filter(e => e.msMeetingId !== undefined && success.map(s => s.msMeetingId).includes(e.msMeetingId));
            dbHelper.deleteEvents(deletedEvents);
            return success.length;
        }
        catch (ex) {
            console.log("Error in deleteTestEvents()");
            console.error(ex);
            return 0;
        }
    }

    public async deleteTestHoldingCalls(verbose = false): Promise<number> {
        try {
            if (verbose) console.log(`Looking for test holding calls in database...`);
            const dbHelper = new CosmosDbHelper();
            const allCalendars = await dbHelper.listCalendars();
            const testCalendars = allCalendars.filter(c => c.id.startsWith("7e57") && c.holdingCalls !== undefined);
            const testHoldingCalls = testCalendars.flatMap(c => c.holdingCalls!).filter(e => e.msMeetingId !== undefined && e.msMeetingId!.indexOf("7e57") === -1);
            if (testHoldingCalls.length === 0) {
                if (verbose) console.log("No holding calls found.");
                return 0;
            }
            if (verbose) console.log(`Found ${testHoldingCalls.length} holding calls, deleting...`);

            const promises = testHoldingCalls.map(hc => this.OnlineMeetingDelete(GraphAccessUserType.WaitingRoomService, hc.msMeetingId));
            const results = await Promise.all(promises);
            const success = results.filter(s => s.statusCode === 204);
            const deletedCalendars: ICalendar[] = [];
            testCalendars.filter(c => c.holdingCalls !== undefined && c.holdingCalls.length > 0).forEach(c => {
                if (c.holdingCalls!.filter(hc => success.map(s => s.msMeetingId).includes(hc.msMeetingId)).length > 0) {
                    deletedCalendars.push(c);
                }
            });
            dbHelper.deleteCalendars(deletedCalendars);
            return success.length;
        }
        catch (ex) {
            console.log("Error in deleteTestHoldingCalls()");
            console.error(ex);
            return 0;
        }
    }
}

export enum GraphAccessUserType {
    ScheduledEventService,
    WaitingRoomService,
    App,
    OnDemandService
}

export interface TeamsAsyncOperation {
    id: string;
    operationType: string;
    createdDateTime: string;
    status: string;
    lastActionDateTime: string;
    attemptsCount: number;
    targetResourceId: string;
    targetResourceLocation: string;
    error: { code: string, message: string };
}

export interface OnlineMeeting {
    id: string;
    creationDateTime: Date;
    startDateTime: Date;
    endDateTime: Date;
    joinWebUrl: string;
    subject: string;
    isBroadcast: boolean;
    autoAdmittedUsers: string;
    isEntryExitAnnounced: boolean;
    allowedPresenters: string;
    videoTeleconferenceId: string;
    externalId: string;
    participants: any;
    lobbyBypassSettings: any;
    audioConferencing: AudioConferencing;
    chatInfo: ChatInfo;
}

export interface AudioConferencing {
    conferenceId?: number;
    tollNumber: string;
    tollFreeNumber: string;
    dialInUrl: string;
    tollNumbers: string[];
    tollFreeNumbers: string[];
}

export interface ChatInfo {
    threadId: string;
    messageId: string;
    replyChainMessageId: string;
}

export interface TeamsListResponse {
    "@odata.context": string | undefined;
    value: {
        id: string;
        displayName: string;
    }[];
}

export interface ConversationMember {
    id: string;
    displayName: string;
    roles: string[];
}

export interface AadConversationMember extends ConversationMember {
    userId: string;
    email: string;
}

export interface TeamMember extends ConversationMember {
    userId: string;
    email: string;
}

export interface Invitation {
    invitedUser: User;
    invitedUserDisplayName: string;
    invitedUserEmailAddress: string;
    invitedUserMessageInfo: any;
    sendInvitationMessage: boolean;
    inviteRedirectUrl: string;
    inviteRedeemUrl: string;
    invitedUserType: string;
    status: string;
}

export interface User {
    "aboutMe": string,
    "accountEnabled": boolean,
    "ageGroup": string,
    "businessPhones": [string],
    "city": string,
    "companyName": string,
    "consentProvidedForMinor": string,
    "country": string,
    "createdDateTime": string,
    "creationType": string,
    "department": string,
    "displayName": string,
    "employeeId": string,
    "employeeType": string,
    "faxNumber": string,
    "givenName": string,
    "hireDate": string,
    "id": string,
    "identities": [any],
    "imAddresses": [string],
    "interests": [string],
    "isResourceAccount": false,
    "jobTitle": string,
    "legalAgeGroupClassification": string,
    "lastPasswordChangeDateTime": string,
    "mail": string,
    "mailboxSettings": any,
    "mailNickname": string,
    "mobilePhone": string,
    "mySite": string,
    "officeLocation": string,
    "otherMails": [string],
    "passwordPolicies": string,
    "passwordProfile": any,
    "pastProjects": [string],
    "postalCode": string,
    "preferredDataLocation": string,
    "preferredLanguage": string,
    "preferredName": string,
    "proxyAddresses": [string],
    "responsibilities": [string],
    "schools": [string],
    "showInAddressList": true,
    "signInSessionsValidFromDateTime": string,
    "skills": [string],
    "state": string,
    "streetAddress": string,
    "surname": string,
    "usageLocation": string,
    "userPrincipalName": string,
    "userType": string,
}

export interface Channel {
    description: string,
    displayName: string,
    id: string,
    isFavoriteByDefault: boolean,
    email: string,
    webUrl: string,
    membershipType: any,
    createdDateTime: string
}

export interface TeamsTab {
    id: string;
    displayName: string;
    webUrl: string;
    configuration: TeamsTabConfiguration;
}

export interface TeamsTabConfiguration {
    entityId: string;
    contentUrl: string;
    websiteUrl: string;
    removeUrl: string;
}