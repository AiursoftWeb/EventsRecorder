using Aiursoft.EventsRecorder.Entities;
using Aiursoft.Scanner.Abstractions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Aiursoft.EventsRecorder.Services.Plugins;

public class PluginService(
    EventsRecorderDbContext context,
    PluginRegistry registry) : IScopedDependency
{
    public async Task<Dictionary<string, string>> GetConfigAsync(string userId, string pluginId)
    {
        var row = await context.PluginConfigs
            .FirstOrDefaultAsync(c => c.UserId == userId && c.PluginId == pluginId);

        if (row == null) return new Dictionary<string, string>();

        return JsonConvert.DeserializeObject<Dictionary<string, string>>(row.ConfigJson)
               ?? new Dictionary<string, string>();
    }

    public async Task SaveConfigAsync(string userId, string pluginId, Dictionary<string, string> values)
    {
        var row = await context.PluginConfigs
            .FirstOrDefaultAsync(c => c.UserId == userId && c.PluginId == pluginId);

        var json = JsonConvert.SerializeObject(values);

        if (row == null)
        {
            context.PluginConfigs.Add(new PluginConfig
            {
                UserId = userId,
                PluginId = pluginId,
                ConfigJson = json
            });
        }
        else
        {
            row.ConfigJson = json;
        }

        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<PluginRunResult>> GetUserPluginResultsAsync(
        string userId, DateTime now)
    {
        var eventTypes = await context.EventTypes
            .Where(t => t.UserId == userId)
            .Include(t => t.Fields.OrderBy(f => f.Order))
            .Include(t => t.Records)
                .ThenInclude(r => r.FieldValues)
            .ToListAsync();

        var results = new List<PluginRunResult>();

        foreach (var plugin in registry.All)
        {
            var config = await GetConfigAsync(userId, plugin.PluginId);
            var isConfigured = IsFullyConfigured(plugin, config);

            IReadOnlyList<PluginMetricResult> metrics = [];
            if (isConfigured)
                metrics = await plugin.ComputeAsync(config, eventTypes, now);

            results.Add(new PluginRunResult
            {
                Plugin = plugin,
                Config = config,
                IsConfigured = isConfigured,
                Metrics = metrics
            });
        }

        return results;
    }

    private static bool IsFullyConfigured(IPlugin plugin, Dictionary<string, string> config) =>
        plugin.ConfigSchema.All(s =>
            config.TryGetValue(s.Key, out var v) && !string.IsNullOrWhiteSpace(v));
}
