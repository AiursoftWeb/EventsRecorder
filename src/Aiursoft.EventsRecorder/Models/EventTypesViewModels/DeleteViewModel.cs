using Aiursoft.UiStack.Layout;

namespace Aiursoft.EventsRecorder.Models.EventTypesViewModels;

public class DeleteViewModel : UiStackLayoutViewModel
{
    public DeleteViewModel()
    {
        PageTitle = "Delete Event Type";
    }

    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int RecordCount { get; set; }
    public int FieldCount { get; set; }
}
