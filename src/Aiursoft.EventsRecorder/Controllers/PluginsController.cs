using System.Security.Claims;
using Aiursoft.EventsRecorder.Entities;
using Aiursoft.EventsRecorder.Models.PluginsViewModels;
using Aiursoft.EventsRecorder.Services;
using Aiursoft.EventsRecorder.Services.Plugins;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.EventsRecorder.Controllers;

[Authorize]
[LimitPerMin]
public class PluginsController(
    EventsRecorderDbContext context,
    PluginRegistry pluginRegistry,
    PluginCalculationService calculationService) : Controller
{
    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [RenderInNavBar(
        NavGroupName = "Insights",
        NavGroupOrder = 3,
        CascadedLinksGroupName = "My Plugins",
        CascadedLinksIcon = "plug",
        CascadedLinksOrder = 1,
        LinkText = "My Plugins",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var configurations = await context.PluginConfigurations
            .Where(c => c.UserId == userId)
            .ToListAsync();

        var cards = new List<PluginCardViewModel>();
        foreach (var plugin in PluginRegistry.All)
        {
            var config = configurations.FirstOrDefault(c => c.PluginId == plugin.Id);
            var card = new PluginCardViewModel
            {
                Definition = plugin,
                IsConfigured = config != null
            };

            if (config != null)
            {
                var result = await calculationService.CalculateAsync(plugin, config);
                if (result != null)
                {
                    card.MetricValues = result.Metrics.Select(m =>
                    {
                        var def = plugin.Metrics.FirstOrDefault(md => md.Id == m.MetricId);
                        return def == null ? null : new MetricValueViewModel { Metric = def, Value = m.Value };
                    }).Where(m => m != null).Cast<MetricValueViewModel>().ToList();
                }
            }

            cards.Add(card);
        }

        return this.StackView(new IndexViewModel { PluginCards = cards });
    }

    [HttpGet]
    public async Task<IActionResult> Configure(string id)
    {
        var plugin = pluginRegistry.GetById(id);
        if (plugin == null) return NotFound();

        var userId = GetUserId();
        var existingConfig = await context.PluginConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.PluginId == id);

        var eventTypes = await context.EventTypes
            .Where(t => t.UserId == userId)
            .ToListAsync();

        var eventTypeOptions = eventTypes
            .Select(t => new SelectListItem(t.Name, t.Id.ToString()))
            .ToList();

        List<SelectListItem> numericFieldOptions = [];
        var selectedEventTypeId = existingConfig?.EventTypeId ?? (eventTypes.FirstOrDefault()?.Id ?? 0);
        if (selectedEventTypeId > 0)
        {
            numericFieldOptions = await context.EventFields
                .Where(f => f.EventTypeId == selectedEventTypeId && f.FieldType == FieldType.Number)
                .Select(f => new SelectListItem(f.Name, f.Id.ToString()))
                .ToListAsync();
        }

        return this.StackView(new ConfigureViewModel
        {
            Plugin = plugin,
            EventTypeId = selectedEventTypeId,
            NumericFieldId = existingConfig?.NumericFieldId,
            EventTypeOptions = eventTypeOptions,
            NumericFieldOptions = numericFieldOptions,
            AlreadyConfigured = existingConfig != null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configure(string id, ConfigureViewModel model)
    {
        var plugin = pluginRegistry.GetById(id);
        if (plugin == null) return NotFound();

        if (!ModelState.IsValid)
        {
            var userId2 = GetUserId();
            var eventTypes2 = await context.EventTypes.Where(t => t.UserId == userId2).ToListAsync();
            model.Plugin = plugin;
            model.EventTypeOptions = eventTypes2.Select(t => new SelectListItem(t.Name, t.Id.ToString())).ToList();
            model.NumericFieldOptions = await context.EventFields
                .Where(f => f.EventTypeId == model.EventTypeId && f.FieldType == FieldType.Number)
                .Select(f => new SelectListItem(f.Name, f.Id.ToString()))
                .ToListAsync();
            return this.StackView(model);
        }

        var userId = GetUserId();

        // Verify the event type belongs to the user
        var eventType = await context.EventTypes
            .FirstOrDefaultAsync(t => t.Id == model.EventTypeId && t.UserId == userId);
        if (eventType == null) return Forbid();

        // Verify the numeric field belongs to the event type (if provided)
        if (model.NumericFieldId.HasValue)
        {
            var field = await context.EventFields
                .FirstOrDefaultAsync(f => f.Id == model.NumericFieldId && f.EventTypeId == model.EventTypeId);
            if (field == null) return Forbid();
        }

        var existing = await context.PluginConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.PluginId == id);

        if (existing != null)
        {
            existing.EventTypeId = model.EventTypeId;
            existing.NumericFieldId = plugin.RequiresNumericField ? model.NumericFieldId : null;
        }
        else
        {
            context.PluginConfigurations.Add(new PluginConfiguration
            {
                UserId = userId,
                PluginId = id,
                EventTypeId = model.EventTypeId,
                NumericFieldId = plugin.RequiresNumericField ? model.NumericFieldId : null
            });
        }

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable(string id)
    {
        var userId = GetUserId();
        var config = await context.PluginConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId && c.PluginId == id);

        if (config != null)
        {
            context.PluginConfigurations.Remove(config);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
}
