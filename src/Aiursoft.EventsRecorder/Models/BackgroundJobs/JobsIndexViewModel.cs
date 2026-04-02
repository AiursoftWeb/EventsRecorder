using Aiursoft.UiStack.Layout;

namespace Aiursoft.EventsRecorder.Models.BackgroundJobs;

public class JobsIndexViewModel : UiStackLayoutViewModel
{
    public IEnumerable<JobInfo> AllRecentJobs { get; init; } = [];
}
