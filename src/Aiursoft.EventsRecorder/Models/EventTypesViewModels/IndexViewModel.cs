using Aiursoft.UiStack.Layout;

namespace Aiursoft.EventsRecorder.Models.EventTypesViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Event Types";
    }

    public required List<EventTypeSummaryViewModel> EventTypes { get; set; }
}

public class EventTypeSummaryViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int RecordCount { get; set; }
    public int FieldCount { get; set; }
    public DateTime CreationTime { get; set; }
}
