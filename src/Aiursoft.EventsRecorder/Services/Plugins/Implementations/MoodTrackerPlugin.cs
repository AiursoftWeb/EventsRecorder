using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>
/// Tracks mood over time using an exponential moving average, simple 7/30-day
/// averages, and standard-deviation volatility.
/// </summary>
public class MoodTrackerPlugin : IPlugin
{
    public string PluginId => "mood_tracker";
    public string Name => "Mood Tracker";
    public string Description =>
        "Tracks mood trends with an exponential moving average (α=0.2), " +
        "7/30-day averages, and 30-day volatility (standard deviation).";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new PluginConfigSchema
        {
            Key         = "event_type_id",
            Label       = "Mood Event Type",
            Description = "Which event type records your mood?",
            Type        = PluginConfigFieldType.EventTypeSelector
        },
        new PluginConfigSchema
        {
            Key                  = "mood_field_id",
            Label                = "Mood Score Field",
            Description          = "Which numeric field stores the mood score (e.g. 1–10)?",
            Type                 = PluginConfigFieldType.FieldSelector,
            EventTypeSelectorKey = "event_type_id",
            FilterFieldType      = FieldType.Number
        }
    ];

    public Task<IReadOnlyList<PluginMetricResult>> ComputeAsync(
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<EventType> userEventTypes,
        DateTime now)
    {
        if (!config.TryGetValue("event_type_id", out var etStr)  || !int.TryParse(etStr, out var etId) ||
            !config.TryGetValue("mood_field_id", out var fStr)   || !int.TryParse(fStr, out var fieldId))
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var eventType = userEventTypes.FirstOrDefault(et => et.Id == etId);
        if (eventType == null)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var points = eventType.Records
            .SelectMany(r => r.FieldValues
                .Where(fv => fv.EventFieldId == fieldId && fv.NumberValue.HasValue)
                .Select(fv => (r.RecordedAt, Value: (double)fv.NumberValue!.Value)))
            .OrderBy(p => p.RecordedAt)
            .ToList();

        if (points.Count == 0)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        // Exponential moving average (α=0.2 → recent entries weighted more)
        const double alpha = 0.2;
        var ema = points[0].Value;
        foreach (var (_, v) in points.Skip(1))
            ema = alpha * v + (1 - alpha) * ema;

        var avg7  = points.Where(p => p.RecordedAt >= now.AddDays(-7))
                          .Select(p => p.Value).DefaultIfEmpty(ema).Average();
        var avg30 = points.Where(p => p.RecordedAt >= now.AddDays(-30))
                          .Select(p => p.Value).DefaultIfEmpty(ema).Average();

        var recent30 = points.Where(p => p.RecordedAt >= now.AddDays(-30))
                             .Select(p => p.Value).ToList();

        var volatility = 0.0;
        if (recent30.Count >= 2)
        {
            var mean = recent30.Average();
            volatility = Math.Sqrt(recent30.Sum(v => (v - mean) * (v - mean)) / (recent30.Count - 1));
        }

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(
        [
            new()
            {
                MetricId    = "ema",
                MetricName  = "Mood EMA",
                Value       = Math.Round(ema, 2),
                Unit        = "pts",
                Explanation = "Exponential moving average (α=0.2). Recent entries weighted more."
            },
            new()
            {
                MetricId    = "avg_7d",
                MetricName  = "7-Day Avg",
                Value       = Math.Round(avg7, 2),
                Unit        = "pts",
                Explanation = "Simple average mood score over the past 7 days."
            },
            new()
            {
                MetricId    = "avg_30d",
                MetricName  = "30-Day Avg",
                Value       = Math.Round(avg30, 2),
                Unit        = "pts",
                Explanation = "Simple average mood score over the past 30 days."
            },
            new()
            {
                MetricId    = "volatility",
                MetricName  = "Mood Volatility",
                Value       = Math.Round(volatility, 2),
                Unit        = "σ",
                Explanation = "Standard deviation over the past 30 days. Lower = more stable."
            }
        ]);
    }
}
