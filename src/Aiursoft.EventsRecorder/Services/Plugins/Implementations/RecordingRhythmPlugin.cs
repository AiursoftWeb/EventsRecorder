using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>Describes the time gaps between records without assuming a local time zone.</summary>
public class RecordingRhythmPlugin : IPlugin
{
    public string PluginId => "recording_rhythm";
    public string Name => "Recording Rhythm";
    public string Description => "See typical gaps and variation between selected events.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new()
        {
            Key = "event_type_ids",
            Label = "Event Types",
            Description = "Which events should be included in the rhythm?",
            Type = PluginConfigFieldType.EventTypeSelectorList
        }
    ];

    public Task<IReadOnlyList<PluginMetricResult>> ComputeAsync(
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<EventType> userEventTypes,
        DateTime now)
    {
        if (!config.TryGetValue("event_type_ids", out var rawIds))
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var ids = PluginHelper.ParseIntList(rawIds);
        if (ids.Count == 0)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var times = userEventTypes.Where(t => ids.Contains(t.Id))
            .SelectMany(t => t.Records)
            .Select(r => r.RecordedAt)
            .Where(t => t <= now)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        if (times.Count < 2)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var gaps = times.Skip(1).Select((t, i) => (t - times[i]).TotalHours).Order().ToList();
        var middle = gaps.Count / 2;
        var median = gaps.Count % 2 == 0 ? (gaps[middle - 1] + gaps[middle]) / 2 : gaps[middle];
        var mean = gaps.Average();
        var stddev = Math.Sqrt(gaps.Sum(g => Math.Pow(g - mean, 2)) / gaps.Count);

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(
        [
            new() { MetricId = "median_gap", MetricName = "Typical Gap", Value = Math.Round(median, 1), Unit = "hours" },
            new() { MetricId = "latest_gap", MetricName = "Latest Gap", Value = Math.Round((times[^1] - times[^2]).TotalHours, 1), Unit = "hours" },
            new() { MetricId = "variation", MetricName = "Gap Variation", Value = mean > 0 ? Math.Round(stddev / mean * 100, 1) : 0, Unit = "%", Explanation = "Lower means more even spacing between records." },
            new() { MetricId = "since_last", MetricName = "Time Since Last Event", Value = Math.Round((now - times[^1]).TotalHours, 1), Unit = "hours" }
        ]);
    }
}
