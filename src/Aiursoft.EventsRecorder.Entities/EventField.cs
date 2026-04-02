using System.ComponentModel.DataAnnotations;

namespace Aiursoft.EventsRecorder.Entities;

public class EventField
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [MinLength(1)]
    public required string Name { get; set; }

    public FieldType FieldType { get; set; }

    public bool IsRequired { get; set; }

    public int Order { get; set; }

    public int EventTypeId { get; set; }

    public EventType? EventType { get; set; }
}
