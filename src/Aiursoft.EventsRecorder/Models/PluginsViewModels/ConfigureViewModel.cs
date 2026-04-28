using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;
using Aiursoft.EventsRecorder.Services.Plugins;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Aiursoft.EventsRecorder.Models.PluginsViewModels;

public class ConfigureViewModel : UiStackLayoutViewModel
{
    public ConfigureViewModel()
    {
        PageTitle = "Configure Plugin";
    }

    public required PluginDefinition Plugin { get; set; }

    [Required]
    [Display(Name = "Event Type")]
    public int EventTypeId { get; set; }

    [Display(Name = "Numeric Field")]
    public int? NumericFieldId { get; set; }

    public List<SelectListItem> EventTypeOptions { get; set; } = [];
    public List<SelectListItem> NumericFieldOptions { get; set; } = [];
    public bool AlreadyConfigured { get; set; }
}
