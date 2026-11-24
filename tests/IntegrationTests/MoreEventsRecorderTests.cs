using Aiursoft.EventsRecorder.Services.Plugins;

namespace Aiursoft.EventsRecorder.Tests.IntegrationTests;

[TestClass]
public class MoreEventsRecorderTests : TestBase
{
    [TestMethod]
    public async Task TestPluginsIndexAndConfigure()
    {
        await LoginAsAdmin();

        var indexResponse = await Http.GetAsync("/Plugins");
        Assert.IsTrue(indexResponse.IsSuccessStatusCode, $"GET /Plugins failed: {indexResponse.StatusCode}");

        var registry = GetService<PluginRegistry>();
        foreach (var plugin in registry.All)
        {
            var configResponse = await Http.GetAsync($"/Plugins/Configure/{plugin.PluginId}");
            Assert.IsTrue(configResponse.IsSuccessStatusCode, $"GET /Plugins/Configure/{plugin.PluginId} failed: {configResponse.StatusCode}");
        }
    }
}
