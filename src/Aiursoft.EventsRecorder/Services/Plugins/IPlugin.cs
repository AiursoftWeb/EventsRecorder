using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins;

public interface IPlugin
{
    string PluginId { get; }

    string Name { get; }

    string Description { get; }

    IReadOnlyList<PluginConfigSchema> ConfigSchema { get; }

    Task<IReadOnlyList<PluginMetricResult>> ComputeAsync(
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<EventType> userEventTypes,
        DateTime now);
}
