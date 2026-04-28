namespace Aiursoft.EventsRecorder.Services.Plugins;

public static class PluginHelper
{
    public static HashSet<int> ParseIntList(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
           .Select(s => int.TryParse(s.Trim(), out var id) ? id : -1)
           .Where(id => id > 0)
           .ToHashSet();

    public static Dictionary<int, int> ParsePairMap(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
           .Select(p => p.Split(':'))
           .Where(p => p.Length == 2
                    && int.TryParse(p[0].Trim(), out _)
                    && int.TryParse(p[1].Trim(), out _))
           .ToDictionary(p => int.Parse(p[0].Trim()), p => int.Parse(p[1].Trim()));
}
