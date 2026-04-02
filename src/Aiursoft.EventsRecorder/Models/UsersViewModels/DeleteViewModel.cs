using System.ComponentModel.DataAnnotations;
using Aiursoft.EventsRecorder.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.EventsRecorder.Models.UsersViewModels;

public class DeleteViewModel : UiStackLayoutViewModel
{
    public DeleteViewModel()
    {
        PageTitle = "Delete User";
    }

    [Display(Name = "User")]
    public required User User { get; set; }
}
