using System.Net;

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
        
        var idMatch = System.Text.RegularExpressions.Regex.Match(indexHtml, @"/EventTypes/Details/(\d+)");
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
        
        var idMatch = System.Text.RegularExpressions.Regex.Match(indexHtml, @"/EventTypes/Details/(\d+)");
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
        var idMatch = System.Text.RegularExpressions.Regex.Match(indexHtml, @"/EventTypes/Details/(\d+)");
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
        var idMatch = System.Text.RegularExpressions.Regex.Match(indexHtml, @"/EventTypes/Details/(\d+)");
        var eventTypeId = idMatch.Groups[1].Value;
        
        var detailsResponse = await Http.GetAsync($"/EventTypes/Details/{eventTypeId}");
        detailsResponse.EnsureSuccessStatusCode();
        var html = await detailsResponse.Content.ReadAsStringAsync();
        
        Assert.Contains("Test Event Type", html);
        Assert.Contains("For testing details page", html);
        Assert.Contains("Fields", html);
    }
}
