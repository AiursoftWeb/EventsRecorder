using System.Security.Claims;
using Aiursoft.EventsRecorder.Entities;
using Aiursoft.EventsRecorder.Models.EventTypesViewModels;
using Aiursoft.EventsRecorder.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.EventsRecorder.Controllers;

[Authorize]
[LimitPerMin]
public class EventTypesController(
    TemplateDbContext context,
    RegularityService regularityService) : Controller
{
    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Events",
        CascadedLinksIcon = "layers",
        CascadedLinksOrder = 2,
        LinkText = "Event Types",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var eventTypes = await context.EventTypes
            .Where(t => t.UserId == userId)
            .Select(t => new EventTypeSummaryViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                RecordCount = t.Records.Count,
                FieldCount = t.Fields.Count,
                CreationTime = t.CreationTime
            })
            .ToListAsync();

        foreach (var eventType in eventTypes)
        {
            var last8Records = await context.EventRecords
                .Where(r => r.EventTypeId == eventType.Id)
                .OrderByDescending(r => r.RecordedAt)
                .Take(8)
                .Select(r => r.RecordedAt)
                .ToListAsync();
            
            eventType.RegularityScore = regularityService.CalculateScore(last8Records);
        }

        return this.StackView(new IndexViewModel { EventTypes = eventTypes });
    }

    public IActionResult Create()
    {
        return this.StackView(new CreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateViewModel model)
    {
        if (!ModelState.IsValid) return this.StackView(model);

        var eventType = new EventType
        {
            Name = model.Name!,
            Description = model.Description,
            UserId = GetUserId()
        };

        context.EventTypes.Add(eventType);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = eventType.Id });
    }

    public async Task<IActionResult> Details(int id, DateTime? start, DateTime? end)
    {
        var userId = GetUserId();
        var eventType = await context.EventTypes
            .Include(t => t.Fields.OrderBy(f => f.Order))
            .Include(t => t.Records)
                .ThenInclude(r => r.FieldValues)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (eventType == null) return NotFound();

        var startTime = start ?? DateTime.UtcNow.AddMonths(-1);
        var endTime = end ?? DateTime.UtcNow.AddDays(1); // Default to tomorrow to include today's records if they don't have time

        var numberFields = eventType.Fields
            .Where(f => f.FieldType == FieldType.Number)
            .ToList();

        var numberSeries = numberFields.Select(field => new NumberSeriesDto
        {
            FieldId = field.Id,
            FieldName = field.Name,
            Points = eventType.Records
                .Where(r => r.RecordedAt >= startTime && r.RecordedAt <= endTime)
                .OrderBy(r => r.RecordedAt)
                .Select(r => new
                {
                    r.RecordedAt,
                    Value = r.FieldValues.FirstOrDefault(fv => fv.EventFieldId == field.Id)?.NumberValue
                })
                .Where(p => p.Value.HasValue)
                .Select(p => new NumberPointDto
                {
                    X = p.RecordedAt,
                    Y = p.Value!.Value
                })
                .ToList()
        })
        .Where(s => s.Points.Count > 0)
        .ToList();

        var booleanFields = eventType.Fields
            .Where(f => f.FieldType == FieldType.Boolean)
            .ToList();

        var booleanSeries = booleanFields.Select(field => new BooleanSeriesDto
        {
            FieldId = field.Id,
            FieldName = field.Name,
            Points = eventType.Records
                .Where(r => r.RecordedAt >= startTime && r.RecordedAt <= endTime)
                .OrderBy(r => r.RecordedAt)
                .Select(r => new
                {
                    r.RecordedAt,
                    Value = r.FieldValues.FirstOrDefault(fv => fv.EventFieldId == field.Id)?.BoolValue
                })
                .Where(p => p.Value.HasValue)
                .Select(p => new BooleanPointDto
                {
                    X = p.RecordedAt,
                    Y = p.Value!.Value
                })
                .ToList()
        })
        .ToList();

        var last8Records = await context.EventRecords
            .Where(r => r.EventTypeId == id)
            .OrderByDescending(r => r.RecordedAt)
            .Take(8)
            .Select(r => r.RecordedAt)
            .ToListAsync();

        var occurrences = eventType.Records
            .Where(r => r.RecordedAt >= startTime && r.RecordedAt <= endTime)
            .GroupBy(r => r.RecordedAt.Date)
            .Select(g => new EventRecordOccurrenceDto
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(o => o.Date)
            .ToList();

        return this.StackView(new DetailsViewModel
        {
            Id = eventType.Id,
            Name = eventType.Name,
            Description = eventType.Description,
            CreationTime = eventType.CreationTime,
            Fields = eventType.Fields.OrderBy(f => f.Order).ToList(),
            RecordCount = eventType.Records.Count,
            RegularityScore = regularityService.CalculateScore(last8Records),
            NumberSeries = numberSeries,
            BooleanSeries = booleanSeries,
            Occurrences = occurrences,
            Start = startTime,
            End = endTime
        });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        var eventType = await context.EventTypes
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (eventType == null) return NotFound();

        return this.StackView(new EditViewModel
        {
            Id = eventType.Id,
            Name = eventType.Name,
            Description = eventType.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditViewModel model)
    {
        if (!ModelState.IsValid) return this.StackView(model);

        var userId = GetUserId();
        var eventType = await context.EventTypes
            .FirstOrDefaultAsync(t => t.Id == model.Id && t.UserId == userId);

        if (eventType == null) return NotFound();

        eventType.Name = model.Name!;
        eventType.Description = model.Description;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = eventType.Id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var eventType = await context.EventTypes
            .Include(t => t.Fields)
            .Include(t => t.Records)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (eventType == null) return NotFound();

        return this.StackView(new DeleteViewModel
        {
            Id = eventType.Id,
            Name = eventType.Name,
            Description = eventType.Description,
            RecordCount = eventType.Records.Count,
            FieldCount = eventType.Fields.Count
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = GetUserId();
        var eventType = await context.EventTypes
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (eventType == null) return NotFound();

        context.EventTypes.Remove(eventType);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
