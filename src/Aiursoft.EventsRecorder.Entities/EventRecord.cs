using System.ComponentModel.DataAnnotations;

namespace Aiursoft.EventsRecorder.Entities;

public class EventRecord
{
    public int Id { get; set; }

    public int EventTypeId { get; set; }

    public EventType? EventType { get; set; }

    [Required]
    public required string UserId { get; set; }

    public User? User { get; set; }

    public DateTime RecordedAt { get; init; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public ICollection<EventFieldValue> FieldValues { get; set; } = new List<EventFieldValue>();
}
