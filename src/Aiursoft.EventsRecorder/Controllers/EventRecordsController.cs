using System.Security.Claims;
using Aiursoft.EventsRecorder.Entities;
using Aiursoft.EventsRecorder.Models.EventRecordsViewModels;
using Aiursoft.EventsRecorder.Services;
using Aiursoft.EventsRecorder.Services.FileStorage;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.EventsRecorder.Controllers;

[Authorize]
[LimitPerMin]
public class EventRecordsController(
    TemplateDbContext context,
    StorageService storageService) : Controller
{
    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Events",
        CascadedLinksIcon = "layers",
        CascadedLinksOrder = 2,
        LinkText = "My Records",
        LinkOrder = 2)]
    public async Task<IActionResult> Index(int? eventTypeId)
    {
        var userId = GetUserId();

        var eventTypes = await context.EventTypes
            .Where(t => t.UserId == userId)
            .Select(t => new EventTypeFilterViewModel { Id = t.Id, Name = t.Name })
            .ToListAsync();

        var query = context.EventRecords
            .Where(r => r.UserId == userId)
            .Include(r => r.EventType)
            .Include(r => r.FieldValues)
            .OrderByDescending(r => r.RecordedAt)
            .AsQueryable();

        if (eventTypeId.HasValue)
        {
            query = query.Where(r => r.EventTypeId == eventTypeId.Value);
        }

        var records = await query
            .Select(r => new RecordSummaryViewModel
            {
                Id = r.Id,
                EventTypeName = r.EventType!.Name,
                EventTypeId = r.EventTypeId,
                RecordedAt = r.RecordedAt,
                Notes = r.Notes,
                FieldValueCount = r.FieldValues.Count
            })
            .ToListAsync();

        return this.StackView(new IndexViewModel
        {
            Records = records,
            EventTypes = eventTypes,
            SelectedEventTypeId = eventTypeId
        });
    }

    public async Task<IActionResult> Record(int? eventTypeId)
    {
        var userId = GetUserId();

        if (!eventTypeId.HasValue)
        {
            var eventTypes = await context.EventTypes
                .Where(t => t.UserId == userId)
                .Select(t => new EventTypeFilterViewModel { Id = t.Id, Name = t.Name })
                .ToListAsync();

            return this.StackView(new SelectTypeViewModel { EventTypes = eventTypes }, "SelectType");
        }

        var eventType = await context.EventTypes
            .Include(t => t.Fields.OrderBy(f => f.Order))
            .FirstOrDefaultAsync(t => t.Id == eventTypeId.Value && t.UserId == userId);

        if (eventType == null) return NotFound();

        var fields = eventType.Fields.Select(f => new FieldInputViewModel
        {
            FieldId = f.Id,
            Name = f.Name,
            FieldType = f.FieldType,
            IsRequired = f.IsRequired
        }).ToList();

        return this.StackView(new RecordViewModel
        {
            EventTypeId = eventType.Id,
            EventTypeName = eventType.Name,
            Fields = fields
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(RecordViewModel model)
    {
        var userId = GetUserId();
        var eventType = await context.EventTypes
            .Include(t => t.Fields.OrderBy(f => f.Order))
            .FirstOrDefaultAsync(t => t.Id == model.EventTypeId && t.UserId == userId);

        if (eventType == null) return NotFound();

        // Rebuild field info for validation and re-rendering
        model.EventTypeName = eventType.Name;
        if (model.Fields == null || model.Fields.Count == 0)
        {
            model.Fields = eventType.Fields.Select(f => new FieldInputViewModel
            {
                FieldId = f.Id,
                Name = f.Name,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired
            }).ToList();
        }
        else
        {
            for (var i = 0; i < model.Fields.Count; i++)
            {
                var dbField = eventType.Fields.FirstOrDefault(f => f.Id == model.Fields[i].FieldId);
                if (dbField != null)
                {
                    model.Fields[i].Name = dbField.Name;
                    model.Fields[i].FieldType = dbField.FieldType;
                    model.Fields[i].IsRequired = dbField.IsRequired;
                }
            }
        }

        // Validate required fields
        foreach (var field in model.Fields.Where(f => f.IsRequired))
        {
            var dbField = eventType.Fields.First(f => f.Id == field.FieldId);
            switch (dbField.FieldType)
            {
                case FieldType.String:
                    if (string.IsNullOrWhiteSpace(field.StringValue))
                        ModelState.AddModelError($"field_{field.FieldId}", $"{field.Name} is required.");
                    break;
                case FieldType.Number:
                    if (string.IsNullOrWhiteSpace(field.NumberValue))
                        ModelState.AddModelError($"field_{field.FieldId}", $"{field.Name} is required.");
                    break;
                case FieldType.File:
                    if (Request.Form.Files[$"file_{field.FieldId}"] == null || Request.Form.Files[$"file_{field.FieldId}"]!.Length == 0)
                        ModelState.AddModelError($"field_{field.FieldId}", $"{field.Name} is required.");
                    break;
                case FieldType.Timespan:
                    if (string.IsNullOrWhiteSpace(field.TimespanHours) && string.IsNullOrWhiteSpace(field.TimespanMinutes))
                        ModelState.AddModelError($"field_{field.FieldId}", $"{field.Name} is required.");
                    break;
            }
        }

        if (!ModelState.IsValid) return this.StackView(model);

        var record = new EventRecord
        {
            EventTypeId = model.EventTypeId,
            UserId = userId,
            Notes = model.Notes
        };

        context.EventRecords.Add(record);
        await context.SaveChangesAsync();

        // Process field values
        foreach (var field in model.Fields)
        {
            var dbField = eventType.Fields.First(f => f.Id == field.FieldId);
            var fieldValue = new EventFieldValue
            {
                EventRecordId = record.Id,
                EventFieldId = field.FieldId
            };

            switch (dbField.FieldType)
            {
                case FieldType.String:
                    fieldValue.StringValue = field.StringValue;
                    break;
                case FieldType.Number:
                    if (decimal.TryParse(field.NumberValue, out var num))
                        fieldValue.NumberValue = num;
                    break;
                case FieldType.Boolean:
                    fieldValue.BoolValue = field.BoolValue;
                    break;
                case FieldType.Timespan:
                    int.TryParse(field.TimespanHours, out var hours);
                    int.TryParse(field.TimespanMinutes, out var minutes);
                    fieldValue.TimespanTicks = new TimeSpan(hours, minutes, 0).Ticks;
                    break;
                case FieldType.File:
                    var file = Request.Form.Files[$"file_{field.FieldId}"];
                    if (file is { Length: > 0 })
                    {
                        var path = $"events/{userId}/{record.Id}";
                        var savedPath = await storageService.Save(
                            Path.Combine(path, file.FileName), file, isVault: true);
                        fieldValue.FileRelativePath = savedPath;
                    }
                    break;
            }

            context.EventFieldValues.Add(fieldValue);
        }

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = record.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = GetUserId();
        var record = await context.EventRecords
            .Include(r => r.EventType)
            .Include(r => r.FieldValues)
                .ThenInclude(fv => fv.EventField)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (record == null) return NotFound();

        var fieldValues = record.FieldValues
            .OrderBy(fv => fv.EventField?.Order)
            .Select(fv => new FieldValueDisplayViewModel
            {
                FieldName = fv.EventField?.Name ?? "Unknown",
                FieldType = fv.EventField?.FieldType ?? FieldType.String,
                StringValue = fv.StringValue,
                NumberValue = fv.NumberValue,
                BoolValue = fv.BoolValue,
                TimespanTicks = fv.TimespanTicks,
                FileRelativePath = fv.FileRelativePath,
                FileDownloadUrl = !string.IsNullOrEmpty(fv.FileRelativePath)
                    ? storageService.RelativePathToInternetUrl(fv.FileRelativePath, isVault: true)
                    : null
            }).ToList();

        return this.StackView(new DetailsViewModel
        {
            Id = record.Id,
            EventTypeName = record.EventType!.Name,
            EventTypeId = record.EventTypeId,
            RecordedAt = record.RecordedAt,
            Notes = record.Notes,
            FieldValues = fieldValues
        });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        var record = await context.EventRecords
            .Include(r => r.EventType)
                .ThenInclude(t => t!.Fields.OrderBy(f => f.Order))
            .Include(r => r.FieldValues)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (record == null) return NotFound();

        var fields = record.EventType!.Fields.Select(f =>
        {
            var existingValue = record.FieldValues.FirstOrDefault(fv => fv.EventFieldId == f.Id);
            var vm = new FieldInputViewModel
            {
                FieldId = f.Id,
                Name = f.Name,
                FieldType = f.FieldType,
                IsRequired = f.IsRequired
            };

            if (existingValue != null)
            {
                switch (f.FieldType)
                {
                    case FieldType.String:
                        vm.StringValue = existingValue.StringValue;
                        break;
                    case FieldType.Number:
                        vm.NumberValue = existingValue.NumberValue?.ToString();
                        break;
                    case FieldType.Boolean:
                        vm.BoolValue = existingValue.BoolValue ?? false;
                        break;
                    case FieldType.Timespan:
                        if (existingValue.TimespanTicks.HasValue)
                        {
                            var ts = TimeSpan.FromTicks(existingValue.TimespanTicks.Value);
                            vm.TimespanHours = ((int)ts.TotalHours).ToString();
                            vm.TimespanMinutes = ts.Minutes.ToString();
                        }
                        break;
                    case FieldType.File:
                        vm.ExistingFilePath = existingValue.FileRelativePath;
                        break;
                }
            }

            return vm;
        }).ToList();

        return this.StackView(new EditViewModel
        {
            Id = record.Id,
            EventTypeId = record.EventTypeId,
            EventTypeName = record.EventType.Name,
            Fields = fields,
            Notes = record.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditViewModel model)
    {
        var userId = GetUserId();
        var record = await context.EventRecords
            .Include(r => r.EventType)
                .ThenInclude(t => t!.Fields.OrderBy(f => f.Order))
            .Include(r => r.FieldValues)
            .FirstOrDefaultAsync(r => r.Id == model.Id && r.UserId == userId);

        if (record == null) return NotFound();

        model.EventTypeName = record.EventType!.Name;

        // Rebuild field info
        if (model.Fields != null)
        {
            for (var i = 0; i < model.Fields.Count; i++)
            {
                var dbField = record.EventType.Fields.FirstOrDefault(f => f.Id == model.Fields[i].FieldId);
                if (dbField != null)
                {
                    model.Fields[i].Name = dbField.Name;
                    model.Fields[i].FieldType = dbField.FieldType;
                    model.Fields[i].IsRequired = dbField.IsRequired;
                }
            }
        }

        if (!ModelState.IsValid) return this.StackView(model);

        record.Notes = model.Notes;

        // Update field values
        foreach (var field in model.Fields ?? [])
        {
            var dbField = record.EventType.Fields.First(f => f.Id == field.FieldId);
            var existingValue = record.FieldValues.FirstOrDefault(fv => fv.EventFieldId == field.FieldId);

            if (existingValue == null)
            {
                existingValue = new EventFieldValue
                {
                    EventRecordId = record.Id,
                    EventFieldId = field.FieldId
                };
                context.EventFieldValues.Add(existingValue);
            }

            switch (dbField.FieldType)
            {
                case FieldType.String:
                    existingValue.StringValue = field.StringValue;
                    break;
                case FieldType.Number:
                    existingValue.NumberValue = decimal.TryParse(field.NumberValue, out var num) ? num : null;
                    break;
                case FieldType.Boolean:
                    existingValue.BoolValue = field.BoolValue;
                    break;
                case FieldType.Timespan:
                    int.TryParse(field.TimespanHours, out var hours);
                    int.TryParse(field.TimespanMinutes, out var minutes);
                    existingValue.TimespanTicks = new TimeSpan(hours, minutes, 0).Ticks;
                    break;
                case FieldType.File:
                    var file = Request.Form.Files[$"file_{field.FieldId}"];
                    if (file is { Length: > 0 })
                    {
                        var path = $"events/{userId}/{record.Id}";
                        var savedPath = await storageService.Save(
                            Path.Combine(path, file.FileName), file, isVault: true);
                        existingValue.FileRelativePath = savedPath;
                    }
                    else if (!string.IsNullOrEmpty(field.ExistingFilePath))
                    {
                        existingValue.FileRelativePath = field.ExistingFilePath;
                    }
                    break;
            }
        }

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = record.Id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var record = await context.EventRecords
            .Include(r => r.EventType)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (record == null) return NotFound();

        return this.StackView(new DeleteViewModel
        {
            Id = record.Id,
            EventTypeName = record.EventType!.Name,
            RecordedAt = record.RecordedAt
        });
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = GetUserId();
        var record = await context.EventRecords
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (record == null) return NotFound();

        context.EventRecords.Remove(record);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
