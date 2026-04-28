namespace Aiursoft.EventsRecorder.Services.Plugins;

public class PluginMetricResult
{
    public required string MetricId { get; init; }

    public required string MetricName { get; init; }

    public required double Value { get; init; }

    public string? Unit { get; init; }

    /// <summary>Plain-text explanation of how this metric was computed.</summary>
    public string? Explanation { get; init; }
}
