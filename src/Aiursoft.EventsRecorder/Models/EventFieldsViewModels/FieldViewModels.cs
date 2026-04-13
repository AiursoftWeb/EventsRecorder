using System.ComponentModel.DataAnnotations;
using Aiursoft.EventsRecorder.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.EventsRecorder.Models.EventFieldsViewModels;

public class CreateViewModel : UiStackLayoutViewModel
{
    public CreateViewModel()
    {
        PageTitle = "Add Field";
    }

    public int EventTypeId { get; set; }
    public string? EventTypeName { get; set; }

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "Field Name")]
    [MaxLength(50, ErrorMessage = "The {0} must be at max {1} characters long.")]
    [MinLength(1, ErrorMessage = "The {0} must be at least {1} characters long.")]
    public string? Name { get; set; }

    [Display(Name = "Field Type")]
    public FieldType FieldType { get; set; }

    [Display(Name = "Required")]
    public bool IsRequired { get; set; }

    [Display(Name = "Enum Values")]
    [MaxLength(1000, ErrorMessage = "The {0} must be at max {1} characters long.")]
    public string? EnumValues { get; set; }
    }

    public class EditViewModel : UiStackLayoutViewModel
    {
    public EditViewModel()
    {
        PageTitle = "Edit Field";
    }

    public int Id { get; set; }
    public int EventTypeId { get; set; }
    public string? EventTypeName { get; set; }

    [Required(ErrorMessage = "The {0} is required.")]
    [Display(Name = "Field Name")]
    [MaxLength(50, ErrorMessage = "The {0} must be at max {1} characters long.")]
    [MinLength(1, ErrorMessage = "The {0} must be at least {1} characters long.")]
    public string? Name { get; set; }

    [Display(Name = "Field Type")]
    public FieldType FieldType { get; set; }

    [Display(Name = "Display Order")]
    public int Order { get; set; }

    [Display(Name = "Required")]
    public bool IsRequired { get; set; }

    [Display(Name = "Enum Values")]
    [MaxLength(1000, ErrorMessage = "The {0} must be at max {1} characters long.")]
    public string? EnumValues { get; set; }
    }
