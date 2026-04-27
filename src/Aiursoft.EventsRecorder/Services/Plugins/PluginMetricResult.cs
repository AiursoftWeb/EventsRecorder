namespace Aiursoft.EventsRecorder.Services.Plugins;

public class PluginMetricResult
{
    public required string MetricId { get; set; }

    public required string MetricName { get; set; }

    public required double Value { get; set; }

    public string? Unit { get; set; }

    /// <summary>Plain-text explanation of how this metric was computed.</summary>
    public string? Explanation { get; set; }
}
