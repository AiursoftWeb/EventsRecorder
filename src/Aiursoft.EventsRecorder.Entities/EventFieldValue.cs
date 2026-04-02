using System.ComponentModel.DataAnnotations;

namespace Aiursoft.EventsRecorder.Entities;

public class EventFieldValue
{
    public int Id { get; set; }

    public int EventRecordId { get; set; }

    public EventRecord? EventRecord { get; set; }

    public int EventFieldId { get; set; }

    public EventField? EventField { get; set; }

    [MaxLength(2000)]
    public string? StringValue { get; set; }

    public decimal? NumberValue { get; set; }

    public bool? BoolValue { get; set; }

    public long? TimespanTicks { get; set; }

    [MaxLength(500)]
    public string? FileRelativePath { get; set; }
}
