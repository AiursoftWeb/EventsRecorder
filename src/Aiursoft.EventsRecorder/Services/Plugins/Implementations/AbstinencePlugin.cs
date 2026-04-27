using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>
/// Tracks self-control for any behavior you want to reduce.
/// Score recovers +10 per day (max 100) and halves on each recorded event.
/// Generalizes the classic "masturbation score" model to any habit.
/// </summary>
public class AbstinencePlugin : IPlugin
{
    public string PluginId => "abstinence";
    public string Name => "Abstinence Score";
    public string Description =>
        "Tracks self-control for any behavior you want to reduce. " +
        "Score recovers +10/day (max 100) and halves on each event.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new PluginConfigSchema
        {
            Key = "event_type_ids",
            Label = "Trigger Event Types",
            Description = "Which event types count as relapse events? (e.g. smoking, drinking, …)",
            Type = PluginConfigFieldType.EventTypeSelectorList
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

        var allRecords = userEventTypes
            .Where(et => ids.Contains(et.Id))
            .SelectMany(et => et.Records)
            .OrderBy(r => r.RecordedAt)
            .ToList();

        double score = 0;
        if (allRecords.Count > 0)
        {
            var recordsByDate = allRecords
                .GroupBy(r => r.RecordedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            for (var date = allRecords[0].RecordedAt.Date; date <= now.Date; date = date.AddDays(1))
            {
                score = Math.Min(score + 10, 100);
                if (recordsByDate.TryGetValue(date, out var count))
                    for (var i = 0; i < count; i++)
                        score /= 2;
            }
        }

        var daysSinceLast = allRecords.Count > 0
            ? (int)(now.Date - allRecords[^1].RecordedAt.Date).TotalDays
            : -1;

        var results = new List<PluginMetricResult>
        {
            new()
            {
                MetricId    = "score",
                MetricName  = "Abstinence Score",
                Value       = Math.Round(score, 1),
                Unit        = "pts",
                Explanation = "Recovers +10/day (max 100). Each event: ÷2."
            }
        };

        if (daysSinceLast >= 0)
            results.Add(new PluginMetricResult
            {
                MetricId    = "days_since_last",
                MetricName  = "Days Since Last Event",
                Value       = daysSinceLast,
                Unit        = "days",
                Explanation = "Days elapsed since the most recent recorded event."
            });

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(results);
    }

    private static HashSet<int> ParseIntList(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
           .Select(s => int.TryParse(s.Trim(), out var id) ? id : -1)
           .Where(id => id > 0)
           .ToHashSet();
}
