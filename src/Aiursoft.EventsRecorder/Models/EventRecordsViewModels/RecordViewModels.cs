using Aiursoft.EventsRecorder.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.EventsRecorder.Models.EventRecordsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "My Records";
    }

    public required List<RecordSummaryViewModel> Records { get; set; }
    public List<EventTypeFilterViewModel> EventTypes { get; set; } = [];
    public int? SelectedEventTypeId { get; set; }
    public List<EventField> SelectedEventTypeFields { get; set; } = [];
}

public class RecordSummaryViewModel
{
    public int Id { get; set; }
    public required string EventTypeName { get; set; }
    public int EventTypeId { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? Notes { get; set; }
    public int FieldValueCount { get; set; }
    public Dictionary<int, FieldValueDisplayViewModel> DynamicFieldValues { get; set; } = [];
}

public class EventTypeFilterViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

public class SelectTypeViewModel : UiStackLayoutViewModel
{
    public SelectTypeViewModel()
    {
        PageTitle = "Record!";
    }

    public required List<EventTypeFilterViewModel> EventTypes { get; set; }
}

public enum RecordingTimeType
{
    RightNow,
    HoursAgo,
    Manual
}

public class RecordViewModel : UiStackLayoutViewModel
{
    public RecordViewModel()
    {
        PageTitle = "Record!";
    }

    public int EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public List<FieldInputViewModel> Fields { get; set; } = [];
    public string? Notes { get; set; }

    public bool ShowAdvanced { get; set; }
    public RecordingTimeType TimeType { get; set; } = RecordingTimeType.RightNow;
    public double HoursAgo { get; set; }
    public DateTime ManualTime { get; set; } = DateTime.UtcNow;
}

public class FieldInputViewModel
{
    public int FieldId { get; set; }
    public string Name { get; set; } = string.Empty;
    public FieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public string? StringValue { get; set; }
    public string? NumberValue { get; set; }
    public bool BoolValue { get; set; }
    public string? TimespanHours { get; set; }
    public string? TimespanMinutes { get; set; }
    // File field: Receives logical path after upload (e.g., "events/userId/recordId/file.pdf")
    // This follows the "逻辑路径" architecture - frontend/database only handle logical paths
    public string? FileValue { get; set; }
}

public class DetailsViewModel : UiStackLayoutViewModel
{
    public DetailsViewModel()
    {
        PageTitle = "Record Details";
    }

    public int Id { get; set; }
    public required string EventTypeName { get; set; }
    public int EventTypeId { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? Notes { get; set; }
    public required List<FieldValueDisplayViewModel> FieldValues { get; set; }
}

public class FieldValueDisplayViewModel
{
    public required string FieldName { get; set; }
    public FieldType FieldType { get; set; }
    public string? StringValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BoolValue { get; set; }
    public long? TimespanTicks { get; set; }
    public string? FileRelativePath { get; set; }
    public string? FileDownloadUrl { get; set; }
}

public class EditViewModel : UiStackLayoutViewModel
{
    public EditViewModel()
    {
        PageTitle = "Edit Record";
    }

    public int Id { get; set; }
    public int EventTypeId { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public List<FieldInputViewModel> Fields { get; set; } = [];
    public string? Notes { get; set; }
}

public class DeleteViewModel : UiStackLayoutViewModel
{
    public DeleteViewModel()
    {
        PageTitle = "Delete Record";
    }

    public int Id { get; set; }
    public required string EventTypeName { get; set; }
    public DateTime RecordedAt { get; set; }
}
