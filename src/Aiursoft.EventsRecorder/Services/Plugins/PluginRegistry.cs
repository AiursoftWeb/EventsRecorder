using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.EventsRecorder.Services.Plugins;

public class PluginRegistry(IEnumerable<IPlugin> plugins) : ISingletonDependency
{
    public IReadOnlyList<IPlugin> All { get; } = plugins.ToList();

    public IPlugin? GetById(string pluginId) =>
        All.FirstOrDefault(p => p.PluginId == pluginId);
}
