using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>Measures progress toward a user-defined number of active days per UTC week.</summary>
public class WeeklyGoalPlugin : IPlugin
{
    public string PluginId => "weekly_goal";
    public string Name => "Weekly Goal";
    public string Description => "Set a weekly target for days with at least one selected event.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new()
        {
            Key = "event_type_ids",
            Label = "Goal Event Types",
            Description = "Which events count toward the goal?",
            Type = PluginConfigFieldType.EventTypeSelectorList
        },
        new()
        {
            Key = "target_days",
            Label = "Target Days per Week",
            Description = "How many days each week do you aim to record one of these events?",
            Type = PluginConfigFieldType.NumberInput,
            MinValue = 1,
            MaxValue = 7
        }
    ];

    public Task<IReadOnlyList<PluginMetricResult>> ComputeAsync(
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<EventType> userEventTypes,
        DateTime now)
    {
        if (!config.TryGetValue("event_type_ids", out var rawIds) ||
            !config.TryGetValue("target_days", out var rawTarget) ||
            !int.TryParse(rawTarget, out var target) || target is < 1 or > 7)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var ids = PluginHelper.ParseIntList(rawIds);
        if (ids.Count == 0)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var start = now.Date.AddDays(-((int)now.DayOfWeek + 6) % 7);
        var activeDays = userEventTypes.Where(t => ids.Contains(t.Id))
            .SelectMany(t => t.Records)
            .Where(r => r.RecordedAt >= start && r.RecordedAt <= now)
            .Select(r => r.RecordedAt.Date)
            .Distinct()
            .Count();

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(
        [
            new() { MetricId = "active_days", MetricName = "Active Days", Value = activeDays, Unit = "days" },
            new() { MetricId = "target_days", MetricName = "Weekly Target", Value = target, Unit = "days" },
            new() { MetricId = "remaining_days", MetricName = "Days Remaining", Value = Math.Max(0, target - activeDays), Unit = "days" },
            new() { MetricId = "progress", MetricName = "Goal Progress", Value = Math.Round(Math.Min(100, activeDays * 100.0 / target), 1), Unit = "%" }
        ]);
    }
}
