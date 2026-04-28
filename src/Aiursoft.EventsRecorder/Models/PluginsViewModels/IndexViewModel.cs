using Aiursoft.UiStack.Layout;
using Aiursoft.EventsRecorder.Services.Plugins;

namespace Aiursoft.EventsRecorder.Models.PluginsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "My Plugins";
    }

    public required List<PluginCardViewModel> PluginCards { get; set; }
}

public class PluginCardViewModel
{
    public required PluginDefinition Definition { get; set; }
    public bool IsConfigured { get; set; }
    public List<MetricValueViewModel> MetricValues { get; set; } = [];
}

public class MetricValueViewModel
{
    public required MetricDefinition Metric { get; set; }
    public double Value { get; set; }
}
