using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins.Implementations;

/// <summary>Compares a numeric outcome according to whether a trigger event occurred in the previous 24 hours.</summary>
public class EventAssociationPlugin : IPlugin
{
    public string PluginId => "event_association";
    public string Name => "Event Association";
    public string Description => "Compare an outcome after a selected event with other outcomes. This does not establish cause and effect.";

    public IReadOnlyList<PluginConfigSchema> ConfigSchema =>
    [
        new()
        {
            Key = "trigger_event_type_id",
            Label = "Trigger Event Type",
            Description = "Which event might precede the outcome?",
            Type = PluginConfigFieldType.EventTypeSelector
        },
        new()
        {
            Key = "outcome_event_type_id",
            Label = "Outcome Event Type",
            Description = "Which event records the outcome?",
            Type = PluginConfigFieldType.EventTypeSelector
        },
        new()
        {
            Key = "outcome_field_id",
            Label = "Outcome Field",
            Description = "Which numeric field contains the outcome value?",
            Type = PluginConfigFieldType.FieldSelector,
            EventTypeSelectorKey = "outcome_event_type_id",
            FilterFieldType = FieldType.Number
        }
    ];

    public Task<IReadOnlyList<PluginMetricResult>> ComputeAsync(
        IReadOnlyDictionary<string, string> config,
        IReadOnlyList<EventType> userEventTypes,
        DateTime now)
    {
        if (!config.TryGetValue("trigger_event_type_id", out var rawTrigger) || !int.TryParse(rawTrigger, out var triggerId) ||
            !config.TryGetValue("outcome_event_type_id", out var rawOutcome) || !int.TryParse(rawOutcome, out var outcomeId) ||
            !config.TryGetValue("outcome_field_id", out var rawField) || !int.TryParse(rawField, out var fieldId) ||
            triggerId == outcomeId)
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var triggerType = userEventTypes.FirstOrDefault(t => t.Id == triggerId);
        var outcomeType = userEventTypes.FirstOrDefault(t => t.Id == outcomeId);
        if (triggerType == null || outcomeType == null ||
            !outcomeType.Fields.Any(f => f.Id == fieldId && f.FieldType == FieldType.Number))
            return Task.FromResult<IReadOnlyList<PluginMetricResult>>([]);

        var triggerTimes = triggerType.Records.Select(r => r.RecordedAt)
            .Where(t => t <= now).Order().ToList();
        var outcomes = outcomeType.Records
            .Where(r => r.RecordedAt <= now)
            .SelectMany(r => r.FieldValues
                .Where(v => v.EventFieldId == fieldId && v.NumberValue.HasValue)
                .Select(v => (r.RecordedAt, Value: (double)v.NumberValue!.Value)))
            .OrderBy(p => p.RecordedAt)
            .ToList();

        var withTrigger = new List<double>();
        var withoutTrigger = new List<double>();
        var triggerIndex = 0;
        foreach (var outcome in outcomes)
        {
            while (triggerIndex < triggerTimes.Count && triggerTimes[triggerIndex] <= outcome.RecordedAt)
                triggerIndex++;

            var preceded = triggerIndex > 0 &&
                           triggerTimes[triggerIndex - 1] >= outcome.RecordedAt.AddHours(-24);
            (preceded ? withTrigger : withoutTrigger).Add(outcome.Value);
        }

        var results = new List<PluginMetricResult>
        {
            new() { MetricId = "with_count", MetricName = "Outcomes After Event", Value = withTrigger.Count, Unit = "records" },
            new() { MetricId = "without_count", MetricName = "Other Outcomes", Value = withoutTrigger.Count, Unit = "records" }
        };

        if (withTrigger.Count >= 3 && withoutTrigger.Count >= 3)
        {
            var withMean = withTrigger.Average();
            var withoutMean = withoutTrigger.Average();
            results.Add(new() { MetricId = "with_average", MetricName = "Average After Event", Value = Math.Round(withMean, 2) });
            results.Add(new() { MetricId = "without_average", MetricName = "Other Average", Value = Math.Round(withoutMean, 2) });
            results.Add(new()
            {
                MetricId = "difference",
                MetricName = "Observed Difference",
                Value = Math.Round(withMean - withoutMean, 2),
                Explanation = "Difference between group averages. Association does not establish cause and effect."
            });
        }

        return Task.FromResult<IReadOnlyList<PluginMetricResult>>(results);
    }
}
