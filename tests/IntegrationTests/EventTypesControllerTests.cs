using System.Net;
using System.Text.RegularExpressions;

namespace Aiursoft.EventsRecorder.Tests.IntegrationTests;

[TestClass]
public class EventTypesControllerTests : TestBase
{
    [TestMethod]
    public async Task IndexRequiresAuthentication()
    {
        var response = await Http.GetAsync("/EventTypes/Index");
        AssertRedirect(response, "/Account/Login", exact: false);
    }

    [TestMethod]
    public async Task IndexReturnsViewWhenAuthenticated()
    {
        await RegisterAndLoginAsync();
        var response = await Http.GetAsync("/EventTypes/Index");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("My Event Types", html);
    }

    [TestMethod]
    public async Task CreateEventTypeSuccessfully()
    {
        await RegisterAndLoginAsync();
        
        var createResponse = await PostForm("/EventTypes/Create", new Dictionary<string, string>
        {
            { "Name", "Daily Workout" },
            { "Description", "Track my exercise sessions" }
        });
        
        AssertRedirect(createResponse, "/EventTypes/Details/", exact: false);
        
        var indexResponse = await Http.GetAsync("/EventTypes/Index");
        var html = await indexResponse.Content.ReadAsStringAsync();
        Assert.Contains("Daily Workout", html);
    }

    [TestMethod]
    public async Task CreateEventTypeWithEmptyNameFails()
    {
        await RegisterAndLoginAsync();
        
        var createResponse = await PostForm("/EventTypes/Create", new Dictionary<string, string>
        {
            { "Name", "" },
            { "Description", "Test description" }
        });
        
        Assert.AreEqual(HttpStatusCode.OK, createResponse.StatusCode);
        var html = await createResponse.Content.ReadAsStringAsync();
        Assert.Contains("The Name is required", html);
    }

    [TestMethod]
    public async Task EditEventTypeSuccessfully()
    {
        await RegisterAndLoginAsync();
        
        var createResponse = await PostForm("/EventTypes/Create", new Dictionary<string, string>
        {
            { "Name", "Original Name" },
            { "Description", "Original Description" }
        });
        AssertRedirect(createResponse, "/EventTypes/Details/", exact: false);
        
        var indexResponse = await Http.GetAsync("/EventTypes/Index");
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        
        var idMatch = Regex.Match(indexHtml, @"/EventTypes/Details/(\d+)");
        Assert.IsTrue(idMatch.Success, "Could not find event type ID");
        var eventTypeId = idMatch.Groups[1].Value;
        
        var editResponse = await PostForm($"/EventTypes/Edit/{eventTypeId}", new Dictionary<string, string>
        {
            { "Id", eventTypeId },
            { "Name", "Updated Name" },
            { "Description", "Updated Description" }
        });
        
        AssertRedirect(editResponse, $"/EventTypes/Details/{eventTypeId}");
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var detailsHtml = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("Updated Name", detailsHtml);
        Assert.Contains("Updated Description", detailsHtml);
    }

    [TestMethod]
    public async Task DeleteEventTypeSuccessfully()
    {
        await RegisterAndLoginAsync();
        
        var createResponse = await PostForm("/EventTypes/Create", new Dictionary<string, string>
        {
            { "Name", "To Be Deleted" },
            { "Description", "This will be removed" }
        });
        AssertRedirect(createResponse, "/EventTypes/Details/", exact: false);
        
        var indexResponse = await Http.GetAsync("/EventTypes/Index");
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        
        var idMatch = Regex.Match(indexHtml, @"/EventTypes/Details/(\d+)");
        Assert.IsTrue(idMatch.Success);
        var eventTypeId = idMatch.Groups[1].Value;
        
        var deleteResponse = await PostForm($"/EventTypes/Delete/{eventTypeId}", new Dictionary<string, string>());
        AssertRedirect(deleteResponse, "/EventTypes");
        
        var finalIndexResponse = await Http.GetAsync("/EventTypes/Index");
        var finalIndexHtml = await finalIndexResponse.Content.ReadAsStringAsync();
        Assert.IsFalse(finalIndexHtml.Contains("To Be Deleted"));
    }

    [TestMethod]
    public async Task UserCannotAccessOtherUsersEventType()
    {
        await RegisterAndLoginAsync();
        
        var createResponse = await PostForm("/EventTypes/Create", new Dictionary<string, string>
        {
            { "Name", "User1 Event Type" },
            { "Description", "Private to user 1" }
        });
        AssertRedirect(createResponse, "/EventTypes/Details/", exact: false);
        
        var indexResponse = await Http.GetAsync("/EventTypes/Index");
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        var idMatch = Regex.Match(indexHtml, @"/EventTypes/Details/(\d+)");
        Assert.IsTrue(idMatch.Success);
        var eventTypeId = idMatch.Groups[1].Value;
        
        await Http.GetAsync("/Account/LogOff");
        
        await RegisterAndLoginAsync();
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        Assert.AreEqual(HttpStatusCode.NotFound, detailsResponse.StatusCode);
    }

    [TestMethod]
    public async Task DetailsPageShowsFieldsAndRecordCount()
    {
        await RegisterAndLoginAsync();
        
        var createResponse = await PostForm("/EventTypes/Create", new Dictionary<string, string>
        {
            { "Name", "Test Event Type" },
            { "Description", "For testing details page" }
        });
        AssertRedirect(createResponse, "/EventTypes/Details/", exact: false);
        
        var indexResponse = await Http.GetAsync("/EventTypes/Index");
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        var idMatch = Regex.Match(indexHtml, @"/EventTypes/Details/(\d+)");
        var eventTypeId = idMatch.Groups[1].Value;
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        detailsResponse.EnsureSuccessStatusCode();
        var html = await detailsResponse.Content.ReadAsStringAsync();
        
        Assert.Contains("Test Event Type", html);
        Assert.Contains("For testing details page", html);
        Assert.Contains("Fields", html);
    }

    [TestMethod]
    public async Task DetailsPageShowsStringPieChart()
    {
        await RegisterAndLoginAsync();
        
        // 1. Create Event Type
        var createResponse = await PostForm("/EventTypes/Create", new Dictionary<string, string>
        {
            { "Name", "String Visualization Test" },
            { "Description", "Testing pie chart for repeating strings" }
        });
        AssertRedirect(createResponse, "/EventTypes/Details/", exact: false);
        
        var indexResponse = await Http.GetAsync("/EventTypes/Index");
        var indexHtml = await indexResponse.Content.ReadAsStringAsync();
        var idMatch = Regex.Match(indexHtml, @"/EventTypes/Details/(\d+)");
        var eventTypeId = idMatch.Groups[1].Value;

        // 2. Create a String Field
        var createFieldResponse = await PostForm("/EventFields/Create", new Dictionary<string, string>
        {
            { "EventTypeId", eventTypeId },
            { "Name", "Status" },
            { "FieldType", "0" }, // String
            { "Order", "1" },
            { "IsRequired", "true" }
        });
        AssertRedirect(createFieldResponse, $"/EventTypes/Details/{eventTypeId}");

        var detailsHtmlWithField = await (await Http.GetAsync($"/EventTypes/Details/{eventTypeId}")).Content.ReadAsStringAsync();
        var fieldIdMatch = Regex.Match(detailsHtmlWithField, @"/EventFields/Edit/(\d+)");
        var fieldId = fieldIdMatch.Groups[1].Value;

        // 3. Create records with only ONE repeating string ("AA")
        // Value "AA" appears 2 times, "AB" and "AC" appear 1 time.
        // This should NOT be visualized.
        string[] singleRepeatingValues = { "AA", "AB", "AA", "AC" };
        foreach (var val in singleRepeatingValues)
        {
            await PostForm("/EventRecords/Record", new Dictionary<string, string>
            {
                { "EventTypeId", eventTypeId },
                { "Fields[0].FieldId", fieldId },
                { "Fields[0].StringValue", val }
            }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        }

        var detailsResponse1 = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        var html1 = await detailsResponse1.Content.ReadAsStringAsync();
        Assert.IsFalse(html1.Contains($"chart_string_{fieldId}"), "Should not visualize when only one string repeats");

        // 4. Create more records to have TWO repeating strings ("AA" and "AB")
        // Now "AA" appears 3 times, "AB" appears 2 times.
        // This SHOULD be visualized.
        string[] moreValues = { "AD", "AB", "AA" };
        foreach (var val in moreValues)
        {
            await PostForm("/EventRecords/Record", new Dictionary<string, string>
            {
                { "EventTypeId", eventTypeId },
                { "Fields[0].FieldId", fieldId },
                { "Fields[0].StringValue", val }
            }, tokenUrl: $"/EventRecords/Record?eventTypeId={eventTypeId}");
        }

        // 5. Check details page for pie chart
        var detailsResponse2 = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        detailsResponse2.EnsureSuccessStatusCode();
        var html2 = await detailsResponse2.Content.ReadAsStringAsync();
        
        // Should contain the canvas for the pie chart
        Assert.Contains($"chart_string_{fieldId}", html2);
        // Should contain the data in the script (labels are lowercased)
        Assert.Contains("\"Label\":\"aa\",\"Count\":3", html2);
        Assert.Contains("\"Label\":\"ab\",\"Count\":2", html2);
        // Should contain "ac" and "ad" in the pie chart data because we now show all strings once the chart is drawn
        Assert.Contains("\"Label\":\"ac\"", html2);
        Assert.Contains("\"Label\":\"ad\"", html2);
    }
}
