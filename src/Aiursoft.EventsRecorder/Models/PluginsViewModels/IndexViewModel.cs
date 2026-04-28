using Aiursoft.EventsRecorder.Services.Plugins;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.EventsRecorder.Models.PluginsViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "My Plugins";
    }

    public required List<PluginRunResult> PluginResults { get; set; }
}
