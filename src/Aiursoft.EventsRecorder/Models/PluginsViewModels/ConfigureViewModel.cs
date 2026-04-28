using Aiursoft.EventsRecorder.Entities;
using Aiursoft.EventsRecorder.Services.Plugins;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.EventsRecorder.Models.PluginsViewModels;

public class ConfigureViewModel : UiStackLayoutViewModel
{
    public ConfigureViewModel()
    {
        PageTitle = "Configure Plugin";
    }

    public required string PluginId { get; set; }

    public required string PluginName { get; set; }

    public string? PluginDescription { get; set; }

    public required List<PluginConfigSchema> ConfigSchema { get; set; }

    public required Dictionary<string, string> CurrentConfig { get; set; }

    public required List<EventType> UserEventTypes { get; set; }
}
