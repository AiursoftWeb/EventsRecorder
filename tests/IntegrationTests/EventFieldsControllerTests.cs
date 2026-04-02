using System.Net;

namespace Aiursoft.EventsRecorder.Tests.IntegrationTests;

[TestClass]
public class EventFieldsControllerTests : TestBase
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

    [TestMethod]
    public async Task CreateFieldRequiresAuthentication()
    {
        var response = await Http.GetAsync("/EventFields/Create?eventTypeId=1");
        AssertRedirect(response, "/Account/Login", exact: false);
    }

    [TestMethod]
    public async Task CreateStringFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId();
        
        var createResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", "Location" },
            { "FieldType", "0" }, // String
            { "IsRequired", "true" }
        });
        
        AssertRedirect(createResponse, $"/EventTypes/Details/{eventTypeId}");
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var html = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Location", html);
        Assert.Contains("String", html);
    }

    [TestMethod]
    public async Task CreateNumberFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId();
        
        var createResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", "Weight" },
            { "FieldType", "1" }, // Number
            { "IsRequired", "false" }
        });
        
        AssertRedirect(createResponse, $"/EventTypes/Details/{eventTypeId}");
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var html = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Weight", html);
        Assert.Contains("Number", html);
    }

    [TestMethod]
    public async Task CreateBooleanFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId();
        
        var createResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", "Success" },
            { "FieldType", "2" }, // Boolean
            { "IsRequired", "true" }
        });
        
        AssertRedirect(createResponse, $"/EventTypes/Details/{eventTypeId}");
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var html = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Success", html);
        Assert.Contains("Boolean", html);
    }

    [TestMethod]
    public async Task CreateTimespanFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId();
        
        var createResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", "Duration" },
            { "FieldType", "3" }, // Timespan
            { "IsRequired", "true" }
        });
        
        AssertRedirect(createResponse, $"/EventTypes/Details/{eventTypeId}");
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var html = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Duration", html);
        Assert.Contains("Timespan", html);
    }

    [TestMethod]
    public async Task CreateFileFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId();
        
        var createResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", "Attachment" },
            { "FieldType", "4" }, // File
            { "IsRequired", "false" }
        });
        
        AssertRedirect(createResponse, $"/EventTypes/Details/{eventTypeId}");
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var html = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Attachment", html);
        Assert.Contains("File", html);
    }

    [TestMethod]
    public async Task CreateFieldWithEmptyNameFails()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId();
        
        var createResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", "" },
            { "FieldType", "0" },
            { "IsRequired", "true" }
        });
        
        Assert.AreEqual(HttpStatusCode.OK, createResponse.StatusCode);
        var html = await createResponse.Content.ReadAsStringAsync();
        Assert.Contains("field is required", html);
    }

    [TestMethod]
    public async Task EditFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId();
        
        var createResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", "Original Name" },
            { "FieldType", "0" },
            { "IsRequired", "true" }
        });
        AssertRedirect(createResponse, $"/EventTypes/Details/{eventTypeId}");
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var detailsHtml = await detailsResponse.Content.ReadAsStringAsync();
        var fieldIdMatch = System.Text.RegularExpressions.Regex.Match(detailsHtml, @"/EventFields/Edit/(\d+)");
        Assert.IsTrue(fieldIdMatch.Success, "Could not find field ID");
        var fieldId = fieldIdMatch.Groups[1].Value;
        
        var editResponse = await PostForm($"/EventFields/Edit/{fieldId}", new Dictionary<string, string>
        {
            { "Id", fieldId },
            { "EventTypeId", eventTypeId },
            { "Name", "Updated Name" },
            { "FieldType", "1" }, // Changed to Number
            { "IsRequired", "false" },
            { "Order", "1" }
        });
        
        AssertRedirect(editResponse, $"/EventTypes/Details/{eventTypeId}");
        
        var finalDetailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var finalHtml = await finalDetailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Updated Name", finalHtml);
        Assert.Contains("Number", finalHtml);
    }

    [TestMethod]
    public async Task DeleteFieldSuccessfully()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId();
        
        var createResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", "To Be Deleted" },
            { "FieldType", "0" },
            { "IsRequired", "true" }
        });
        AssertRedirect(createResponse, $"/EventTypes/Details/{eventTypeId}");
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var detailsHtml = await detailsResponse.Content.ReadAsStringAsync();
        var fieldIdMatch = System.Text.RegularExpressions.Regex.Match(detailsHtml, @"/EventFields/Edit/(\d+)");
        Assert.IsTrue(fieldIdMatch.Success);
        var fieldId = fieldIdMatch.Groups[1].Value;
        
        var deleteResponse = await PostForm($"/EventFields/Delete/{fieldId}", new Dictionary<string, string>());
        AssertRedirect(deleteResponse, $"/EventTypes/Details/{eventTypeId}");
        
        var finalDetailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var finalHtml = await finalDetailsResponse.Content.ReadAsStringAsync();
        Assert.IsFalse(finalHtml.Contains("To Be Deleted"));
    }

    [TestMethod]
    public async Task UserCannotAddFieldToOtherUsersEventType()
    {
        await RegisterAndLoginAsync();
        var eventTypeId = await CreateEventTypeAndGetId("User1 Event Type");
        
        await Http.GetAsync("/Account/LogOff");
        
        await RegisterAndLoginAsync();
        
        var createResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", "Unauthorized Field" },
            { "FieldType", "0" },
            { "IsRequired", "true" }
        });
        
        Assert.AreEqual(HttpStatusCode.NotFound, createResponse.StatusCode);
    }
}
