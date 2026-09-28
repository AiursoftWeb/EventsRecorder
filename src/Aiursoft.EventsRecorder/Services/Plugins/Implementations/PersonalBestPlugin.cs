using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>Finds the maximum of a selected numeric field and compares it with the latest value.</summary>
public class PersonalBestPlugin : IPlugin
{
    public string PluginId => "personal_best";
    public string Name => "Personal Best";
    public string Description => "Track your highest recorded value and your latest result.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new()
        {
            Key = "event_type_id",
            Label = "Event Type",
            Description = "Which event contains the result?",
            Type = PluginConfigFieldType.EventTypeSelector
        },
        new()
        {
            Key = "value_field_id",
            Label = "Result Field",
            Description = "Which numeric field should be maximized?",
            Type = PluginConfigFieldType.FieldSelector,
            EventTypeSelectorKey = "event_type_id",
            FilterFieldType = FieldType.Number
        }
    ];

    public Task<IReadOnlyList<PluginMetricResult>> ComputeAsync(
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<EventType> userEventTypes,
        DateTime now)
    {
        if (!config.TryGetValue("event_type_id", out var rawType) || !int.TryParse(rawType, out var typeId) ||
            !config.TryGetValue("value_field_id", out var rawField) || !int.TryParse(rawField, out var fieldId))
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var eventType = userEventTypes.FirstOrDefault(t => t.Id == typeId);
        if (eventType == null || !eventType.Fields.Any(f => f.Id == fieldId && f.FieldType == FieldType.Number))
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var points = eventType.Records
            .Where(r => r.RecordedAt <= now)
            .SelectMany(r => r.FieldValues
                .Where(v => v.EventFieldId == fieldId && v.NumberValue.HasValue)
                .Select(v => (r.RecordedAt, v.NumberValue!.Value)))
            .OrderBy(p => p.RecordedAt)
            .ToList();
        if (points.Count == 0)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var best = points.MaxBy(p => p.Value)!;
        var latest = points[^1];
        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(
        [
            new() { MetricId = "best", MetricName = "Personal Best", Value = (double)best.Value },
            new() { MetricId = "latest", MetricName = "Latest Result", Value = (double)latest.Value },
            new() { MetricId = "gap", MetricName = "Gap to Best", Value = (double)(best.Value - latest.Value) },
            new() { MetricId = "days_since_best", MetricName = "Days Since Best", Value = (now.Date - points.Last(p => p.Value == best.Value).RecordedAt.Date).TotalDays, Unit = "days" }
        ]);
    }
}
