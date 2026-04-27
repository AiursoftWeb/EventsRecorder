using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>
/// Tracks body weight over time with 7/30-day moving averages and a
/// linear regression trend (kg per week) over the past 30 days.
/// </summary>
public class WeightTrendPlugin : IPlugin
{
    public string PluginId => "weight_trend";
    public string Name => "Weight Trend";
    public string Description =>
        "Tracks body weight with moving averages and a weekly trend direction " +
        "computed via linear regression over the past 30 days.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new PluginConfigSchema
        {
            Key         = "event_type_id",
            Label       = "Weigh-In Event Type",
            Description = "Which event type records your weight?",
            Type        = PluginConfigFieldType.EventTypeSelector
        },
        new PluginConfigSchema
        {
            Key                  = "weight_field_id",
            Label                = "Weight Field",
            Description          = "Which numeric field stores the weight value?",
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
        if (!config.TryGetValue("event_type_id", out var etStr)   || !int.TryParse(etStr, out var etId) ||
            !config.TryGetValue("weight_field_id", out var fStr)  || !int.TryParse(fStr, out var fieldId))
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

        var latest = points[^1].Value;

        var avg7  = points.Where(p => p.RecordedAt >= now.AddDays(-7))
                          .Select(p => p.Value).DefaultIfEmpty(latest).Average();
        var avg30 = points.Where(p => p.RecordedAt >= now.AddDays(-30))
                          .Select(p => p.Value).DefaultIfEmpty(latest).Average();

        var trendKgPerWeek = LinearRegressionSlope(
            points.Where(p => p.RecordedAt >= now.AddDays(-30)).ToList()) * 7;

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(
        [
            new()
            {
                MetricId    = "latest",
                MetricName  = "Latest Weight",
                Value       = Math.Round(latest, 2),
                Unit        = "kg",
                Explanation = "Most recent recorded weight."
            },
            new()
            {
                MetricId    = "avg_7d",
                MetricName  = "7-Day Average",
                Value       = Math.Round(avg7, 2),
                Unit        = "kg",
                Explanation = "Average weight over the past 7 days."
            },
            new()
            {
                MetricId    = "avg_30d",
                MetricName  = "30-Day Average",
                Value       = Math.Round(avg30, 2),
                Unit        = "kg",
                Explanation = "Average weight over the past 30 days."
            },
            new()
            {
                MetricId    = "trend",
                MetricName  = "Weekly Trend",
                Value       = Math.Round(trendKgPerWeek, 3),
                Unit        = "kg/week",
                Explanation = "Rate of change over 30 days. Positive = gaining, negative = losing."
            }
        ]);
    }

    /// <summary>Returns the slope (Δvalue / Δday) from a linear regression on the points.</summary>
    private static double LinearRegressionSlope(
        IReadOnlyList<(DateTime RecordedAt, double Value)> points)
    {
        if (points.Count < 2) return 0;

        var origin = points[0].RecordedAt;
        var xs = points.Select(p => (p.RecordedAt - origin).TotalDays).ToList();
        var ys = points.Select(p => p.Value).ToList();
        var n  = xs.Count;

        var sumX  = xs.Sum();
        var sumY  = ys.Sum();
        var sumXy = xs.Zip(ys, (x, y) => x * y).Sum();
        var sumX2 = xs.Sum(x => x * x);
        var denom = n * sumX2 - sumX * sumX;

        return Math.Abs(denom) < 1e-9 ? 0 : (n * sumXy - sumX * sumY) / denom;
    }
}
