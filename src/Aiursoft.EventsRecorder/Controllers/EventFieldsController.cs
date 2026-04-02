using System.Security.Claims;
using Aiursoft.EventsRecorder.Entities;
using Aiursoft.EventsRecorder.Models.EventFieldsViewModels;
using Aiursoft.EventsRecorder.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.EventsRecorder.Controllers;

[Authorize]
[LimitPerMin]
public class EventFieldsController(TemplateDbContext context) : Controller
{
    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Create(int eventTypeId)
    {
        var userId = GetUserId();
        var eventType = await context.EventTypes
            .FirstOrDefaultAsync(t => t.Id == eventTypeId && t.UserId == userId);
        if (eventType == null) return NotFound();

        return this.StackView(new CreateViewModel
        {
            EventTypeId = eventTypeId,
            EventTypeName = eventType.Name
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateViewModel model)
    {
        var userId = GetUserId();
        var eventType = await context.EventTypes
            .Include(t => t.Fields)
            .FirstOrDefaultAsync(t => t.Id == model.EventTypeId && t.UserId == userId);
        if (eventType == null) return NotFound();

        model.EventTypeName = eventType.Name;
        if (!ModelState.IsValid) return this.StackView(model);

        var maxOrder = eventType.Fields.Any() ? eventType.Fields.Max(f => f.Order) : 0;

        var field = new EventField
        {
            Name = model.Name!,
            FieldType = model.FieldType,
            IsRequired = model.IsRequired,
            Order = maxOrder + 1,
            EventTypeId = model.EventTypeId
        };

        context.EventFields.Add(field);
        await context.SaveChangesAsync();

        return RedirectToAction("Details", "EventTypes", new { id = model.EventTypeId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        var field = await context.EventFields
            .Include(f => f.EventType)
            .FirstOrDefaultAsync(f => f.Id == id && f.EventType!.UserId == userId);
        if (field == null) return NotFound();

        return this.StackView(new EditViewModel
        {
            Id = field.Id,
            EventTypeId = field.EventTypeId,
            EventTypeName = field.EventType!.Name,
            Name = field.Name,
            FieldType = field.FieldType,
            IsRequired = field.IsRequired,
            Order = field.Order
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditViewModel model)
    {
        var userId = GetUserId();
        var field = await context.EventFields
            .Include(f => f.EventType)
            .FirstOrDefaultAsync(f => f.Id == model.Id && f.EventType!.UserId == userId);
        if (field == null) return NotFound();

        model.EventTypeName = field.EventType!.Name;
        if (!ModelState.IsValid) return this.StackView(model);

        field.Name = model.Name!;
        field.FieldType = model.FieldType;
        field.IsRequired = model.IsRequired;
        field.Order = model.Order;
        await context.SaveChangesAsync();

        return RedirectToAction("Details", "EventTypes", new { id = field.EventTypeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var field = await context.EventFields
            .Include(f => f.EventType)
            .FirstOrDefaultAsync(f => f.Id == id && f.EventType!.UserId == userId);
        if (field == null) return NotFound();

        var eventTypeId = field.EventTypeId;
        context.EventFields.Remove(field);
        await context.SaveChangesAsync();

        return RedirectToAction("Details", "EventTypes", new { id = eventTypeId });
    }
}
