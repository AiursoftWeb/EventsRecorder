namespace Aiursoft.EventsRecorder.Services.Plugins;

public enum PluginConfigFieldType
{
    /// <summary>Single EventType dropdown. Stored as "42".</summary>
    EventTypeSelector,

    /// <summary>Multi EventType checkboxes. Stored as "42,17,8".</summary>
    EventTypeSelectorList,

    /// <summary>Single field dropdown, depends on an EventTypeSelector. Stored as "7".</summary>
    FieldSelector,

    /// <summary>
    /// One field dropdown per EventType selected by a paired EventTypeSelectorList.
    /// Stored as "42:7,17:23".
    /// </summary>
    FieldSelectorPerSource,
}
