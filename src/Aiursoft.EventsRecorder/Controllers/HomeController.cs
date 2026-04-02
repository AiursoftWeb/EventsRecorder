using Aiursoft.EventsRecorder.Models.HomeViewModels;
using Aiursoft.EventsRecorder.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.EventsRecorder.Controllers;

[LimitPerMin]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return this.SimpleView(new IndexViewModel());
    }
}
