using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>Totals a duration field across selected event types.</summary>
public class TimeInvestmentPlugin : IPlugin
{
    public string PluginId => "time_investment";
    public string Name => "Time Investment";
    public string Description => "See how many hours you spent on selected activities.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new()
        {
            Key = "event_type_ids",
            Label = "Activity Event Types",
            Description = "Which event types contain time spent?",
            Type = PluginConfigFieldType.EventTypeSelectorList
        },
        new()
        {
            Key = "duration_fields",
            Label = "Duration Field per Event Type",
            Description = "Choose the duration field for each activity.",
            Type = PluginConfigFieldType.FieldSelectorPerSource,
            EventTypeSelectorKey = "event_type_ids",
            FilterFieldType = FieldType.Timespan
        }
    ];

    public Task<IReadOnlyList<PluginMetricResult>> ComputeAsync(
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<EventType> userEventTypes,
        DateTime now)
    {
        if (!config.TryGetValue("event_type_ids", out var rawIds) ||
            !config.TryGetValue("duration_fields", out var rawFields))
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var ids = PluginHelper.ParseIntList(rawIds);
        var fields = PluginHelper.ParsePairMap(rawFields);
        if (ids.Count == 0 || fields.Count == 0)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var start = now.Date.AddDays(-((int)now.DayOfWeek + 6) % 7);
        var entries = userEventTypes
            .Where(t => ids.Contains(t.Id) && fields.ContainsKey(t.Id))
            .SelectMany(t => t.Records.SelectMany(r => r.FieldValues
                .Where(v => v.EventFieldId == fields[t.Id] && v.TimespanTicks is >= 0)
                .Select(v => (r.RecordedAt, Hours: TimeSpan.FromTicks(v.TimespanTicks!.Value).TotalHours))))
            .Where(e => e.RecordedAt <= now)
            .ToList();

        var thisWeek = entries.Where(e => e.RecordedAt >= start).Sum(e => e.Hours);
        var lastWeek = entries.Where(e => e.RecordedAt >= start.AddDays(-7) && e.RecordedAt < start)
            .Sum(e => e.Hours);
        var last30Days = entries.Where(e => e.RecordedAt >= now.AddDays(-30)).Sum(e => e.Hours);

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(
        [
            new() { MetricId = "this_week", MetricName = "Hours This Week", Value = Math.Round(thisWeek, 1), Unit = "hours" },
            new() { MetricId = "last_week", MetricName = "Hours Last Week", Value = Math.Round(lastWeek, 1), Unit = "hours" },
            new() { MetricId = "last_30_days", MetricName = "Hours in 30 Days", Value = Math.Round(last30Days, 1), Unit = "hours" }
        ]);
    }
}
