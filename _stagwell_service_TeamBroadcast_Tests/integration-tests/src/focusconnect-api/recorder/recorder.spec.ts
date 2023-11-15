import { expect } from "chai";
import { CosmosDbHelper } from "../../helpers/cosmos-helper";
import { CalendarHelper } from "../calendar/calendar-helper";
import { RecorderHelper } from "./recorder-helper";

const calendarHelper = new CalendarHelper();
const recorderHelper = new RecorderHelper();
const dbHelper = new CosmosDbHelper();

describe("Recorder", async function (): Promise<void> {

    before(async function () {
        console.log("Recorder Before...");
    });

    after(async function () {
        console.log("Recorder After...");
        //await dbHelper.deleteTestData(false, ["Departments", "PersonnelRoles", "Titles", "Locations"]);
    });

    this.timeout(360000);
   
    it("Recorder - Create, update, and delete", async function (): Promise<void> {
        // CREATE facility
        const facility = await dbHelper.createTestFacility({ createTeam: true });

        // CREATE department
        const department = await dbHelper.createTestLookup("Departments", "department");

        // CREATE focus user
        const user = await dbHelper.createTestUser({ department, createMsUser: true });

        // CREATE recorder
        const recorder = await recorderHelper.createRecorder(department);

        expect(recorder.departmentId).equal(department.id);
        expect(recorder.departmentName).contains(department.name);
        expect(recorder.email).contains(recorder.locationName.toLowerCase());

        // UPDATE recorder
        const updatedRecorder = await recorderHelper.updateRecorder(recorder.id, {
            departmentId: recorder.departmentId,
            displayName: `${recorder.displayName}_UPDATED`,
            provisioningStatus: "Provisioned",
            locationName: `${recorder.locationName}_UPDATED`,
            recordingTypeId: recorder.recordingTypeId,
            streamTypeId: recorder.streamTypeId
        });
        
        expect(updatedRecorder.displayName).equals( `${recorder.displayName}_UPDATED`);
        expect(updatedRecorder.locationName).equals( `${recorder.locationName}_UPDATED`);
        
        // CREATE calendar
        const calendarRequest = await calendarHelper.createCalendarRequestBody({ facility, department, focusUsers: [user], recorders: [recorder] });
        const createCalendarResponse = await calendarHelper.createCalendar(calendarRequest);       

        expect(createCalendarResponse.data.recorders![0]).equals(recorder.email);

        // DELETE recorder
        await recorderHelper.deleteRecorder(recorder.id);
        
        const getCalendarResponse = await calendarHelper.getCalendar(createCalendarResponse.data.externalCalendarId);
        
        expect(getCalendarResponse.data?.recorders?.length).equals(0);
    });    

    it("Recorder - Retrieve all", async function (): Promise<void> {
        await recorderHelper.retrieveRecorderList();
    });

    it("Recorder - Retrieve", async function (): Promise<void> {
        const recorder = await dbHelper.createTestRecorder();
        const getResponse = await recorderHelper.getRecorder(recorder.id);
        expect(getResponse.data.id).equals(recorder.id);
        expect(getResponse.data.email).equals(recorder.email);
    });

    describe("Recorder Exceptions", async function (): Promise<void> {
        it("Recorder - Retrieve - 404 Recorder not found", async function (): Promise<void> {
            await recorderHelper.getRecorder("UNKNOWN_RECORDER_ID", 404);
        });
    });
});