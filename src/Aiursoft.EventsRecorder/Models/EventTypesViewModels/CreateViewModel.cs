using System.ComponentModel.DataAnnotations;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.EventsRecorder.Models.EventTypesViewModels;

public class CreateViewModel : UiStackLayoutViewModel
{
    public CreateViewModel()
    {
        PageTitle = "Create Event Type";
    }

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "Name")]
    [MaxLength(50, ErrorMessage = "The {0} must be at max {1} characters long.")]
    [MinLength(1, ErrorMessage = "The {0} must be at least {1} characters long.")]
    public string? Name { get; set; }

    [Display(Name = "Description")]
    [MaxLength(200, ErrorMessage = "The {0} must be at max {1} characters long.")]
    public string? Description { get; set; }
}
