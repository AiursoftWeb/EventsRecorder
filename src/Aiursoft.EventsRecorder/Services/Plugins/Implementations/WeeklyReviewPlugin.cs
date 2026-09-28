using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>Summarizes selected events during the current and previous UTC weeks.</summary>
public class WeeklyReviewPlugin : IPlugin
{
    public string PluginId => "weekly_review";
    public string Name => "Weekly Review";
    public string Description => "Compare this week's event activity with last week.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new()
        {
            Key = "event_type_ids",
            Label = "Event Types",
            Description = "Which events belong in your weekly review?",
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

        var start = now.Date.AddDays(-((int)now.DayOfWeek + 6) % 7);
        var previousStart = start.AddDays(-7);
        var records = userEventTypes.Where(t => ids.Contains(t.Id))
            .SelectMany(t => t.Records)
            .Where(r => r.RecordedAt >= previousStart && r.RecordedAt <= now)
            .ToList();

        var current = records.Where(r => r.RecordedAt >= start).ToList();
        var previousCount = records.Count(r => r.RecordedAt < start);
        var activeDays = current.Select(r => r.RecordedAt.Date).Distinct().Count();

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(
        [
            new() { MetricId = "this_week", MetricName = "This Week", Value = current.Count, Unit = "records" },
            new() { MetricId = "last_week", MetricName = "Last Week", Value = previousCount, Unit = "records" },
            new() { MetricId = "change", MetricName = "Change From Last Week", Value = current.Count - previousCount, Unit = "records" },
            new() { MetricId = "active_days", MetricName = "Active Days This Week", Value = activeDays, Unit = "days" }
        ]);
    }
}
