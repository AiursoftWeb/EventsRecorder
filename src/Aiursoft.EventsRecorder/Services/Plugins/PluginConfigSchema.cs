using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins;

public class PluginConfigSchema
{
    /// <summary>Config key used for storage and form binding.</summary>
    public required string Key { get; init; }

    public required string Label { get; init; }

    public required string Description { get; init; }

    public PluginConfigFieldType Type { get; init; }

    /// <summary>
    /// For FieldSelector / FieldSelectorPerSource:
    /// the Key of the EventTypeSelector(List) this depends on.
    /// </summary>
    public string? EventTypeSelectorKey { get; init; }

    /// <summary>
    /// For FieldSelector / FieldSelectorPerSource:
    /// only display fields whose FieldType matches this value.
    /// </summary>
    public FieldType? FilterFieldType { get; init; }
}
