using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>
/// Tracks sleep patterns: average duration, sleep consistency (bedtime variance),
/// and total sleep debt over the past 7 days.
/// </summary>
public class SleepTrackerPlugin : IPlugin
{
    public string PluginId => "sleep_tracker";
    public string Name => "Sleep Analysis";
    public string Description =>
        "Analyzes sleep duration and consistency. Tracks your average sleep time and bedtime stability over the past 30 days.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new()
        {
            Key         = "event_type_id",
            Label       = "Sleep Event Type",
            Description = "Which event type records your sleep? (e.g. 'Sleep')",
            Type        = PluginConfigFieldType.EventTypeSelector
        },
        new()
        {
            Key                  = "duration_field_id",
            Label                = "Duration Field",
            Description          = "Which field records the sleep duration? (Timespan type recommended)",
            Type                 = PluginConfigFieldType.FieldSelector,
            EventTypeSelectorKey = "event_type_id",
            FilterFieldType      = FieldType.Timespan
        }
    ];

    public Task<IReadOnlyList<PluginMetricResult>> ComputeAsync(
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<EventType> userEventTypes,
        DateTime now)
    {
        if (!config.TryGetValue("event_type_id", out var etStr) || !int.TryParse(etStr, out var etId) ||
            !config.TryGetValue("duration_field_id", out var fStr) || !int.TryParse(fStr, out var fieldId))
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var eventType = userEventTypes.FirstOrDefault(et => et.Id == etId);
        if (eventType == null)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var sleepLogs = eventType.Records
            .SelectMany(r => r.FieldValues
                .Where(fv => fv.EventFieldId == fieldId && fv.TimespanTicks.HasValue)
                .Select(fv => new { r.RecordedAt, Duration = TimeSpan.FromTicks(fv.TimespanTicks!.Value) }))
            .OrderBy(p => p.RecordedAt)
            .ToList();

        if (sleepLogs.Count == 0)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var recent30 = sleepLogs.Where(l => l.RecordedAt >= now.AddDays(-30)).ToList();
        if (recent30.Count == 0)
            recent30 = [sleepLogs[^1]];

        var avgDurationHours = recent30.Average(l => l.Duration.TotalHours);

        // Bedtime consistency: variance of the 'time of day' when recorded (assuming recorded at wake up)
        // We use the recorded time minus duration to estimate bedtime
        var bedtimes = recent30.Select(l => (l.RecordedAt - l.Duration).TimeOfDay.TotalHours).ToList();
        var meanBedtime = bedtimes.Average();
        var variance = bedtimes.Sum(b => Math.Pow(b - meanBedtime, 2)) / bedtimes.Count;
        var stability = Math.Max(0, 100 - Math.Sqrt(variance) * 20); // Heuristic stability score

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(
        [
            new()
            {
                MetricId    = "avg_duration",
                MetricName  = "Average Sleep",
                Value       = Math.Round(avgDurationHours, 1),
                Unit        = "hours",
                Explanation = "Your average daily sleep duration over the past 30 days."
            },
            new()
            {
                MetricId    = "stability",
                MetricName  = "Sleep Stability",
                Value       = Math.Round(stability, 1),
                Unit        = "%",
                Explanation = "How consistent your bedtime is. Higher is more stable."
            }
        ]);
    }
}
// Localization markers for dotlang:
// Localizer["Sleep Analysis"]
// Localizer["Analyzes sleep duration and consistency. Tracks your average sleep time and bedtime stability over the past 30 days."]
// Localizer["Sleep Event Type"]
// Localizer["Which event type records your sleep? (e.g. 'Sleep')"]
// Localizer["Duration Field"]
// Localizer["Which field records the sleep duration? (Timespan type recommended)"]
// Localizer["Average Sleep"]
// Localizer["Your average daily sleep duration over the past 30 days."]
// Localizer["Sleep Stability"]
// Localizer["How consistent your bedtime is. Higher is more stable."]
