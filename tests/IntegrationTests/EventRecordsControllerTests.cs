using System.Net;

namespace Aiursoft.EventsRecorder.Tests.IntegrationTests;

[TestClass]
public class EventRecordsControllerTests : TestBase
{
    private async Task<string> CreateEventTypeAndGetId(string name = "Test Event Type")
    {
        var createResponse = await PostForm("/EventTypes/Create", new Dictionary<string, string>
        {
            { "Name", name },
            { "Description", "Test Description" }
        });
        AssertRedirect(createResponse, "/EventTypes/Details/", exact: false);
        
        var redirectLocation = createResponse.Headers.Location?.OriginalString ?? string.Empty;
        var idMatch = System.Text.RegularExpressions.Regex.Match(redirectLocation, @"EventTypes/Details/(\d+)");
        Assert.IsTrue(idMatch.Success, $"Could not find event type ID in redirect: {redirectLocation}");
        return idMatch.Groups[1].Value;
    }

    private async Task<string> CreateField(string eventTypeId, string name, string fieldType, bool isRequired)
    {
        var createResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", name },
            { "FieldType", fieldType },
            { "IsRequired", isRequired.ToString().ToLower() }
        });
        AssertRedirect(createResponse, $"/EventTypes/Details/{eventTypeId}");
        
        // Get the field ID from the details page
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var detailsHtml = await detailsResponse.Content.ReadAsStringAsync();
        var fieldIdMatches = System.Text.RegularExpressions.Regex.Matches(detailsHtml, @"/EventFields/Edit/(\d+)");
        
        // Return the last created field ID
        if (fieldIdMatches.Count > 0)
        {
            return fieldIdMatches[fieldIdMatches.Count - 1].Groups[1].Value;
        }
        
        throw new Exception($"Could not find field ID after creating field '{name}'");
    }

    [TestMethod]
    public async Task IndexRequiresAuthentication()
    {
        var response = await Http.GetAsync("/EventRecords/Index");
        AssertRedirect(response, "/Account/Login", exact: false);
    }

    [TestMethod]
    public async Task IndexReturnsViewWhenAuthenticated()
    {
        await RegisterAndLoginAsync();
        var response = await Http.GetAsync("/EventRecords/Index");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("My Records", html);
    }

    [TestMethod]
    public async Task RecordGetWithoutEventTypeIdShowsSelectType()
    {
        await RegisterAndLoginAsync();
        await CreateEventTypeAndGetId("Event Type 1");
        await CreateEventTypeAndGetId("Event Type 2");
        
        var response = await Http.GetAsync("/EventRecords/Record");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Select an event type to record", html);
        Assert.Contains("Event Type 1", html);
        Assert.Contains("Event Type 2", html);
    }

    [TestMethod]
    public async Task RecordGetWithEventTypeIdShowsForm()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("Workout");
        await CreateField(eventTypeId, "Exercise", "0", true); // String, required
        await CreateField(eventTypeId, "Reps", "1", false); // Number, optional
        
        var response = await Http.GetAsync($"/EventRecords/Record?eventTypeId={eventTypeId}");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        
        Assert.Contains("Record:", html);
        Assert.Contains("Workout", html);
        Assert.Contains("Exercise", html);
        Assert.Contains("Reps", html);
    }

    [TestMethod]
    public async Task RecordStringFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("Note");
        var fieldId = await CreateField(eventTypeId, "Content", "0", true); // String
        
        var recordResponse = await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "EventTypeName", "Note" },
            { "Fields[0].FieldId", fieldId },
            { "Fields[0].Name", "Content" },
            { "Fields[0].FieldType", "0" },
            { "Fields[0].IsRequired", "true" },
            { "Fields[0].StringValue", "Hello World" }
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        
        AssertRedirect(recordResponse, "/EventRecords/Details/", exact: false);
        
        var indexResponse = await Http.GetAsync("/EventRecords/Index");
        var html = await indexResponse.Content.ReadAsStringAsync();
        Assert.Contains("Note", html);
    }

    [TestMethod]
    public async Task RecordNumberFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("Weight Log");
        var fieldId = await CreateField(eventTypeId, "Weight", "1", true); // Number
        
        var recordResponse = await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "EventTypeName", "Weight Log" },
            { "Fields[0].FieldId", fieldId },
            { "Fields[0].Name", "Weight" },
            { "Fields[0].FieldType", "1" },
            { "Fields[0].IsRequired", "true" },
            { "Fields[0].NumberValue", "75.5" }
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        
        AssertRedirect(recordResponse, "/EventRecords/Details/", exact: false);
    }

    [TestMethod]
    public async Task RecordBooleanFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("Task");
        var fieldId = await CreateField(eventTypeId, "Completed", "2", false); // Boolean
        
        var recordResponse = await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "EventTypeName", "Task" },
            { "Fields[0].FieldId", fieldId },
            { "Fields[0].Name", "Completed" },
            { "Fields[0].FieldType", "2" },
            { "Fields[0].IsRequired", "false" },
            { "Fields[0].BoolValue", "true" }
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        
        AssertRedirect(recordResponse, "/EventRecords/Details/", exact: false);
    }

    [TestMethod]
    public async Task RecordTimespanFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("Activity");
        var fieldId = await CreateField(eventTypeId, "Duration", "3", true); // Timespan
        
        var recordResponse = await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "EventTypeName", "Activity" },
            { "Fields[0].FieldId", fieldId },
            { "Fields[0].Name", "Duration" },
            { "Fields[0].FieldType", "3" },
            { "Fields[0].IsRequired", "true" },
            { "Fields[0].TimespanHours", "2" },
            { "Fields[0].TimespanMinutes", "30" }
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        
        AssertRedirect(recordResponse, "/EventRecords/Details/", exact: false);
    }

    [TestMethod]
    public async Task RecordRequiredFieldValidation()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("Survey");
        var fieldId = await CreateField(eventTypeId, "Response", "0", true); // String, required
        
        var recordResponse = await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Fields[0].FieldId", fieldId },
            { "Fields[0].StringValue", "" } // Empty required field
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        
        Assert.AreEqual(HttpStatusCode.OK, recordResponse.StatusCode);
        var html = await recordResponse.Content.ReadAsStringAsync();
        Assert.Contains("Response", html);
        Assert.Contains("required", html);
    }

    [TestMethod]
    public async Task ViewRecordDetails()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("Meeting");
        var fieldId = await CreateField(eventTypeId, "Topic", "0", true);
        
        await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Notes", "Important meeting" },
            { "Fields[0].FieldId", fieldId },
            { "Fields[0].StringValue", "Project Planning" }
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        
        var indexResponse = await Http.GetAsync("/EventRecords/Index");
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        var detailsMatch = System.Text.RegularExpressions.Regex.Match(indexHtml, @"/EventRecords/Details/(\d+)");
        
        if (detailsMatch.Success)
        {
            var recordId = detailsMatch.Groups[1].Value;
            var detailsResponse = await Http.GetAsync($"/EventRecords/Details/{recordId}");
            detailsResponse.EnsureSuccessStatusCode();
            var detailsHtml = await detailsResponse.Content.ReadAsStringAsync();
            
            Assert.Contains("Meeting", detailsHtml);
            Assert.Contains("Project Planning", detailsHtml);
            Assert.Contains("Important meeting", detailsHtml);
        }
    }

    [TestMethod]
    public async Task EditRecordSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("Journal");
        var fieldId = await CreateField(eventTypeId, "Entry", "0", true);
        
        await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Fields[0].FieldId", fieldId },
            { "Fields[0].StringValue", "Original Entry" }
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        
        var indexResponse = await Http.GetAsync("/EventRecords/Index");
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        var recordIdMatch = System.Text.RegularExpressions.Regex.Match(indexHtml, @"/EventRecords/Details/(\d+)");
        
        if (recordIdMatch.Success)
        {
            var recordId = recordIdMatch.Groups[1].Value;
            
            var editResponse = await PostForm($"/EventRecords/Edit/{recordId}", new Dictionary<string, string>
            {
                { "Id", recordId },
                { "EventTypeId", eventTypeId },
                { "Notes", "Updated notes" },
                { "Fields[0].FieldId", fieldId },
                { "Fields[0].StringValue", "Updated Entry" }
            }, tokenUrl: $"/EventRecords/Edit/{recordId}");
            
            AssertRedirect(editResponse, $"/EventRecords/Details/{recordId}");
            
            var detailsResponse = await Http.GetAsync($"/EventRecords/Details/{recordId}");
            var detailsHtml = await detailsResponse.Content.ReadAsStringAsync();
            Assert.Contains("Updated Entry", detailsHtml);
        }
    }

    [TestMethod]
    public async Task DeleteRecordSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("Temporary");
        var fieldId = await CreateField(eventTypeId, "Data", "0", false);
        
        await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Fields[0].FieldId", fieldId },
            { "Fields[0].StringValue", "To Be Deleted" }
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        
        var indexResponse = await Http.GetAsync("/EventRecords/Index");
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        var recordIdMatch = System.Text.RegularExpressions.Regex.Match(indexHtml, @"/EventRecords/Details/(\d+)");
        
        if (recordIdMatch.Success)
        {
            var recordId = recordIdMatch.Groups[1].Value;
            
            var deleteResponse = await PostForm($"/EventRecords/Delete/{recordId}", new Dictionary<string, string>(), tokenUrl: $"/EventRecords/Delete/{recordId}");
            AssertRedirect(deleteResponse, "/EventRecords/Index");
            
            var finalResponse = await Http.GetAsync("/EventRecords/Index");
            var finalHtml = await finalResponse.Content.ReadAsStringAsync();
            Assert.IsFalse(finalHtml.Contains($"/EventRecords/Details/{recordId}"));
        }
    }

    [TestMethod]
    public async Task FilterRecordsByEventType()
    {
        await RegisterAndLoginAsync();
        var eventTypeId1 = await CreateEventTypeAndGetId("Type A");
        var fieldId1 = await CreateField(eventTypeId1, "Field A", "0", false);
        
        var eventTypeId2 = await CreateEventTypeAndGetId("Type B");
        var fieldId2 = await CreateField(eventTypeId2, "Field B", "0", false);
        
        await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId1 },
            { "Fields[0].FieldId", fieldId1 },
            { "Fields[0].StringValue", "Record A" }
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId1}");
        
        await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId2 },
            { "Fields[0].FieldId", fieldId2 },
            { "Fields[0].StringValue", "Record B" }
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId2}");
        
        var filteredResponse = await Http.GetAsync($"/EventRecords/Index?eventTypeId={eventTypeId1}");
        filteredResponse.EnsureSuccessStatusCode();
        var html = await filteredResponse.Content.ReadAsStringAsync();
        
        Assert.Contains("Type A", html);
    }

    [TestMethod]
    public async Task UserCannotAccessOtherUsersRecord()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("Private Event");
        var fieldId = await CreateField(eventTypeId, "Secret", "0", false);
        
        await PostForm("/EventRecords/Record", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Fields[0].FieldId", fieldId },
            { "Fields[0].StringValue", "Confidential" }
        }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        
        var indexResponse = await Http.GetAsync("/EventRecords/Index");
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        var recordIdMatch = System.Text.RegularExpressions.Regex.Match(indexHtml, @"/EventRecords/Details/(\d+)");
        
        if (recordIdMatch.Success)
        {
            var recordId = recordIdMatch.Groups[1].Value;
            
            await Http.GetAsync("/Account/LogOff");
            await RegisterAndLoginAsync();
            
            var detailsResponse = await Http.GetAsync($"/EventRecords/Details/{recordId}");
            Assert.AreEqual(HttpStatusCode.NotFound, detailsResponse.StatusCode);
        }
    }
}
