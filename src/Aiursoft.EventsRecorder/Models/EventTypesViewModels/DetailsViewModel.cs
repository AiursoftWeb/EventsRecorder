using Aiursoft.EventsRecorder.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.EventsRecorder.Models.EventTypesViewModels;

public class DetailsViewModel : UiStackLayoutViewModel
{
    public DetailsViewModel()
    {
        PageTitle = "Event Type Details";
    }

    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreationTime { get; set; }
    public required List<EventField> Fields { get; set; }
    public int RecordCount { get; set; }
    public double RegularityScore { get; set; }
    public List<NumberSeriesDto> NumberSeries { get; set; } = [];
    public List<BooleanSeriesDto> BooleanSeries { get; set; } = [];
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class BooleanSeriesDto
{
    public int FieldId { get; set; }
    public required string FieldName { get; set; }
    public List<BooleanPointDto> Points { get; set; } = [];
}

public class BooleanPointDto
{
    public DateTime X { get; set; }
    public bool Y { get; set; }
}

public class NumberSeriesDto
{
    public int FieldId { get; set; }
    public required string FieldName { get; set; }
    public List<NumberPointDto> Points { get; set; } = [];
}

public class NumberPointDto
{
    public DateTime X { get; set; }
    public decimal Y { get; set; }
}
