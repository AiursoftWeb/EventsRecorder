namespace Aiursoft.EventsRecorder.Services.Plugins;

public class PluginRunResult
{
    public required IPlugin Plugin { get; set; }

    public required Dictionary<string, string> Config { get; set; }

    public required bool IsConfigured { get; set; }

    public required IReadOnlyList<PluginMetricResult> Metrics { get; set; }
}
