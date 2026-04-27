using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>
/// Computes fitness (CTL), fatigue (ATL), and form using the classic training-load model.
/// Supports multiple exercise event types, each with its own calories field.
/// </summary>
public class ExercisePlugin : IPlugin
{
    public string PluginId => "exercise";
    public string Name => "Exercise Analytics";
    public string Description =>
        "Computes Fitness (42-day load), Fatigue (7-day load), and Form " +
        "using the CTL/ATL training-load model. Supports multiple sources.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new()
        {
            Key         = "source_event_types",
            Label       = "Exercise Event Types",
            Description = "Which event types represent exercise sessions?",
            Type        = PluginConfigFieldType.EventTypeSelectorList
        },
        new()
        {
            Key                  = "calories_fields",
            Label                = "Calories Field per Event Type",
            Description          = "For each selected event type, which numeric field records calories burned?",
            Type                 = PluginConfigFieldType.FieldSelectorPerSource,
            EventTypeSelectorKey = "source_event_types",
            FilterFieldType      = FieldType.Number
        }
    ];

    public Task<IReadOnlyList<PluginMetricResult>> ComputeAsync(
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<EventType> userEventTypes,
        DateTime now)
    {
        if (!config.TryGetValue("calories_fields", out var mappingStr) || string.IsNullOrWhiteSpace(mappingStr))
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        // "42:7,17:23" → Dictionary<eventTypeId, fieldId>
        var fieldMap = PluginHelper.ParsePairMap(mappingStr);
        if (fieldMap.Count == 0)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var allPoints = userEventTypes
            .Where(et => fieldMap.ContainsKey(et.Id))
            .SelectMany(et =>
            {
                var fieldId = fieldMap[et.Id];
                return et.Records.SelectMany(r =>
                    r.FieldValues
                     .Where(fv => fv.EventFieldId == fieldId && fv.NumberValue.HasValue)
                     .Select(fv => (r.RecordedAt, Value: (double)fv.NumberValue!.Value)));
            })
            .ToList();

        var fitness = allPoints
            .Where(p => p.RecordedAt.Date >= now.Date.AddDays(-42))
            .Select(p => p.Value)
            .DefaultIfEmpty(0)
            .Average();

        var fatigue = allPoints
            .Where(p => p.RecordedAt.Date >= now.Date.AddDays(-7))
            .Select(p => p.Value)
            .DefaultIfEmpty(0)
            .Average();

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(
        [
            new()
            {
                MetricId    = "fitness",
                MetricName  = "Fitness (CTL)",
                Value       = Math.Round(fitness, 1),
                Unit        = "kcal/session",
                Explanation = "Average calories per session over the past 42 days."
            },
            new()
            {
                MetricId    = "fatigue",
                MetricName  = "Fatigue (ATL)",
                Value       = Math.Round(fatigue, 1),
                Unit        = "kcal/session",
                Explanation = "Average calories per session over the past 7 days."
            },
            new()
            {
                MetricId    = "form",
                MetricName  = "Form (CTL − ATL)",
                Value       = Math.Round(fitness - fatigue, 1),
                Unit        = "kcal/session",
                Explanation = "Fitness minus Fatigue. Positive = fresh; negative = fatigued."
            }
        ]);
    }
}
