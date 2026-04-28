namespace Aiursoft.EventsRecorder.Services.Plugins;

public class PluginRunResult
{
    public required IPlugin Plugin { get; init; }

    public required Dictionary<string, string> Config { get; init; }

    public required bool IsConfigured { get; init; }

    public required IReadOnlyList<PluginMetricResult> Metrics { get; init; }
}
