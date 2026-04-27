using Aiursoft.EventsRecorder.Entities;

namespace Aiursoft.EventsRecorder.Services.Plugins;

public class PluginConfigSchema
{
    /// <summary>Config key used for storage and form binding.</summary>
    public required string Key { get; set; }

    public required string Label { get; set; }

    public required string Description { get; set; }

    public PluginConfigFieldType Type { get; set; }

    /// <summary>
    /// For FieldSelector / FieldSelectorPerSource:
    /// the Key of the EventTypeSelector(List) this depends on.
    /// </summary>
    public string? EventTypeSelectorKey { get; set; }

    /// <summary>
    /// For FieldSelector / FieldSelectorPerSource:
    /// only display fields whose FieldType matches this value.
    /// </summary>
    public FieldType? FilterFieldType { get; set; }
}
