using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>
/// Tracks daily habit completion: current streak, longest streak, and 30-day completion rate.
/// Any day with at least one record counts as "done".
/// </summary>
public class HabitStreakPlugin : IPlugin
{
    public string PluginId => "habit_streak";
    public string Name => "Habit Streak";
    public string Description =>
        "Tracks your daily habit streak, all-time longest streak, and 30-day completion rate. " +
        "Any day with at least one record counts as a completed day.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new PluginConfigSchema
        {
            Key         = "event_type_ids",
            Label       = "Habit Event Types",
            Description = "Which event types count as completing the habit for the day?",
            Type        = PluginConfigFieldType.EventTypeSelectorList
        }
    ];

    public Task<IReadOnlyList<PluginMetricResult>> ComputeAsync(
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<EventType> userEventTypes,
        DateTime now)
    {
        if (!config.TryGetValue("event_type_ids", out var idsStr) || string.IsNullOrWhiteSpace(idsStr))
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var ids = ParseIntList(idsStr);

        var doneDates = userEventTypes
            .Where(et => ids.Contains(et.Id))
            .SelectMany(et => et.Records)
            .Select(r => r.RecordedAt.Date)
            .ToHashSet();

        // Current streak: walk back from today (or yesterday if today not yet done)
        var currentStreak = 0;
        var checkDate = doneDates.Contains(now.Date) ? now.Date : now.Date.AddDays(-1);
        while (doneDates.Contains(checkDate))
        {
            currentStreak++;
            checkDate = checkDate.AddDays(-1);
        }

        // Longest streak: linear scan over sorted done dates
        var longestStreak = 0;
        if (doneDates.Count > 0)
        {
            var sorted = doneDates.OrderBy(d => d).ToList();
            var run = 1;
            longestStreak = 1;
            for (var i = 1; i < sorted.Count; i++)
            {
                run = sorted[i] == sorted[i - 1].AddDays(1) ? run + 1 : 1;
                longestStreak = Math.Max(longestStreak, run);
            }
        }

        var doneInLast30 = Enumerable.Range(0, 30)
            .Count(offset => doneDates.Contains(now.Date.AddDays(-offset)));

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(
        [
            new PluginMetricResult
            {
                MetricId    = "current_streak",
                MetricName  = "Current Streak",
                Value       = currentStreak,
                Unit        = "days",
                Explanation = "Consecutive days with at least one record, up to today."
            },
            new PluginMetricResult
            {
                MetricId    = "longest_streak",
                MetricName  = "Longest Streak",
                Value       = longestStreak,
                Unit        = "days",
                Explanation = "All-time longest consecutive daily streak."
            },
            new PluginMetricResult
            {
                MetricId    = "completion_rate_30d",
                MetricName  = "30-Day Completion",
                Value       = Math.Round(doneInLast30 / 30.0 * 100, 1),
                Unit        = "%",
                Explanation = "Percentage of days in the past 30 days with at least one record."
            }
        ]);
    }

    private static HashSet<int> ParseIntList(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
           .Select(s => int.TryParse(s.Trim(), out var id) ? id : -1)
           .Where(id => id > 0)
           .ToHashSet();
}
