using System.ComponentModel.DataAnnotations;

namespace Aiursoft.EventsRecorder.Entities;

public class PluginConfiguration
{
    public int Id { get; set; }

    [Required]
    public required string UserId { get; set; }

    public User? User { get; set; }

    [Required]
    [MaxLength(50)]
    public required string PluginId { get; set; }

    public int EventTypeId { get; set; }

    public EventType? EventType { get; set; }

    public int? NumericFieldId { get; set; }

    public EventField? NumericField { get; set; }
}
