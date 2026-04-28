using System.Security.Claims;
using Aiursoft.EventsRecorder.Entities;
using Aiursoft.EventsRecorder.Models.PluginsViewModels;
using Aiursoft.EventsRecorder.Services;
using Aiursoft.EventsRecorder.Services.Plugins;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.EventsRecorder.Controllers;

[Authorize]
[LimitPerMin]
public class PluginsController(
    EventsRecorderDbContext context,
    PluginService pluginService,
    PluginRegistry pluginRegistry) : Controller
{
    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Insights",
        CascadedLinksIcon = "plug",
        CascadedLinksOrder = 3,
        LinkText = "My Plugins",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var results = await pluginService.GetUserPluginResultsAsync(userId, DateTime.UtcNow);
        return this.StackView(new IndexViewModel { PluginResults = results.ToList() });
    }

    public async Task<IActionResult> Configure(string id)
    {
        var plugin = pluginRegistry.GetById(id);
        if (plugin == null) return NotFound();

        var userId = GetUserId();
        var config = await pluginService.GetConfigAsync(userId, id);

        var eventTypes = await context.EventTypes
            .Where(t => t.UserId == userId)
            .Include(t => t.Fields.OrderBy(f => f.Order))
            .OrderBy(t => t.Name)
            .ToListAsync();

        return this.StackView(new ConfigureViewModel
        {
            PluginId          = plugin.PluginId,
            PluginName        = plugin.Name,
            PluginDescription = plugin.Description,
            ConfigSchema      = plugin.ConfigSchema.ToList(),
            CurrentConfig     = config,
            UserEventTypes    = eventTypes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configure(string id, IFormCollection form)
    {
        var plugin = pluginRegistry.GetById(id);
        if (plugin == null) return NotFound();

        var config = plugin.ConfigSchema
            .ToDictionary(s => s.Key, s => form[s.Key].FirstOrDefault() ?? string.Empty);

        await pluginService.SaveConfigAsync(GetUserId(), id, config);
        return RedirectToAction(nameof(Index));
    }
}
