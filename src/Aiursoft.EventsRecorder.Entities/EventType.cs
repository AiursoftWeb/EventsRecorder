using System.ComponentModel.DataAnnotations;

namespace Aiursoft.EventsRecorder.Entities;

public class EventType
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    [MinLength(1)]
    public required string Name { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [Required]
    public required string UserId { get; set; }

    public User? User { get; set; }

    public DateTime CreationTime { get; init; } = DateTime.UtcNow;

    public ICollection<EventField> Fields { get; set; } = new List<EventField>();

    public ICollection<EventRecord> Records { get; set; } = new List<EventRecord>();
}
