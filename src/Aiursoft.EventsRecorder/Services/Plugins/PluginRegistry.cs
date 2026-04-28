using Aiursoft.EventsRecorder.Entities;
using Aiursoft.Scanner.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.EventsRecorder.Services.Plugins;

public record MetricDefinition(
    string Id,
    string Name,
    string Unit,
    string Description);

public record PluginDefinition(
    string Id,
    string Name,
    string Description,
    bool RequiresNumericField,
    List<MetricDefinition> Metrics);

public class PluginMetricResult(string metricId, double value)
{
    public string MetricId { get; } = metricId;
    public double Value { get; } = value;
}

public class PluginResult(string pluginId, List<PluginMetricResult> metrics)
{
    public string PluginId { get; } = pluginId;
    public List<PluginMetricResult> Metrics { get; } = metrics;
}

public class PluginRegistry : ISingletonDependency
{
    public static readonly List<PluginDefinition> All =
    [
        new PluginDefinition(
            Id: "abstinence",
            Name: "Abstinence Score",
            Description: "Tracks self-control for any behavior you want to reduce. Score recovers +10/day (max 100) and halves on each event.",
            RequiresNumericField: false,
            Metrics:
            [
                new MetricDefinition("abstinence_score", "Abstinence Score", "pts", "Recovers +10/day (max 100). Each event: ÷2."),
                new MetricDefinition("days_since_last", "Days Since Last Event", "days", "Days elapsed since the most recent recorded event.")
            ]),

        new PluginDefinition(
            Id: "exercise",
            Name: "Exercise Analytics",
            Description: "Computes Fitness (42-day load), Fatigue (7-day load), and Form using the CTL/ATL training-load model. Supports multiple sources.",
            RequiresNumericField: true,
            Metrics:
            [
                new MetricDefinition("fitness_ctl", "Fitness (CTL)", "kcal/session", "Average calories per session over the past 42 days."),
                new MetricDefinition("fatigue_atl", "Fatigue (ATL)", "kcal/session", "Average calories per session over the past 7 days."),
                new MetricDefinition("form_tsb", "Form (CTL \u2212 ATL)", "kcal/session", "Fitness minus Fatigue. Positive = fresh; negative = fatigued.")
            ]),

        new PluginDefinition(
            Id: "habit_streak",
            Name: "Habit Streak",
            Description: "Tracks your daily habit streak, all-time longest streak, and 30-day completion rate. Any day with at least one record counts as a completed day.",
            RequiresNumericField: false,
            Metrics:
            [
                new MetricDefinition("current_streak", "Current Streak", "days", "Consecutive days with at least one recorded event."),
                new MetricDefinition("longest_streak", "Longest Streak", "days", "All-time longest consecutive streak."),
                new MetricDefinition("completion_rate_30d", "30-Day Completion Rate", "%", "Percentage of days in the past 30 days with at least one record.")
            ]),

        new PluginDefinition(
            Id: "mood_tracker",
            Name: "Mood Tracker",
            Description: "Tracks mood trends with an exponential moving average (\u03b1=0.2), 7/30-day averages, and 30-day volatility (standard deviation).",
            RequiresNumericField: true,
            Metrics:
            [
                new MetricDefinition("mood_ema", "Mood EMA", "pts", "Exponential moving average of mood (\u03b1=0.2)."),
                new MetricDefinition("mood_avg_7d", "7-Day Average", "pts", "Average mood over the past 7 days."),
                new MetricDefinition("mood_avg_30d", "30-Day Average", "pts", "Average mood over the past 30 days."),
                new MetricDefinition("mood_volatility_30d", "30-Day Volatility", "pts", "Standard deviation of mood over the past 30 days.")
            ]),

        new PluginDefinition(
            Id: "sleep_tracker",
            Name: "Sleep Analysis",
            Description: "Analyzes sleep duration and consistency. Tracks your average sleep time and bedtime stability over the past 30 days.",
            RequiresNumericField: true,
            Metrics:
            [
                new MetricDefinition("avg_sleep_30d", "Average Sleep Duration", "hrs", "Average sleep duration over the past 30 days."),
                new MetricDefinition("bedtime_stability", "Bedtime Stability", "hrs \u03c3", "Standard deviation of sleep duration over the past 30 days.")
            ]),

        new PluginDefinition(
            Id: "weight_trend",
            Name: "Weight Trend",
            Description: "Tracks body weight with moving averages and a weekly trend direction computed via linear regression over the past 30 days.",
            RequiresNumericField: true,
            Metrics:
            [
                new MetricDefinition("latest_weight", "Latest Weight", "kg", "Most recent recorded weight."),
                new MetricDefinition("avg_weight_7d", "7-Day Average", "kg", "Average weight over the past 7 days."),
                new MetricDefinition("avg_weight_30d", "30-Day Average", "kg", "Average weight over the past 30 days."),
                new MetricDefinition("weekly_trend", "Weekly Trend", "kg/week", "Rate of change over 30 days. Positive = gaining, negative = losing.")
            ])
    ];

    public PluginDefinition? GetById(string id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
}

public class PluginCalculationService(EventsRecorderDbContext context) : IScopedDependency
{
    public async Task<PluginResult?> CalculateAsync(PluginDefinition plugin, PluginConfiguration config)
    {
        return plugin.Id switch
        {
            "abstinence" => await CalculateAbstinenceAsync(config),
            "exercise" => await CalculateExerciseAsync(config),
            "habit_streak" => await CalculateHabitStreakAsync(config),
            "mood_tracker" => await CalculateMoodTrackerAsync(config),
            "sleep_tracker" => await CalculateSleepTrackerAsync(config),
            "weight_trend" => await CalculateWeightTrendAsync(config),
            _ => null
        };
    }

    private async Task<PluginResult> CalculateAbstinenceAsync(PluginConfiguration config)
    {
        var now = DateTime.UtcNow;
        var records = await context.EventRecords
            .Where(r => r.EventTypeId == config.EventTypeId)
            .OrderBy(r => r.RecordedAt)
            .Select(r => r.RecordedAt)
            .ToListAsync();

        double score = 100.0;
        DateTime? lastEvent = null;
        if (records.Count > 0)
        {
            lastEvent = records[^1];
            for (var i = 0; i < records.Count; i++)
            {
                score /= 2.0;
                if (i + 1 < records.Count)
                {
                    var daysBetween = (records[i + 1] - records[i]).TotalDays;
                    score = Math.Min(100, score + daysBetween * 10.0);
                }
            }
            var daysSinceLast = (now - records[^1]).TotalDays;
            score = Math.Min(100, score + daysSinceLast * 10.0);
        }

        var daysSinceLastEvent = lastEvent.HasValue ? (now - lastEvent.Value).TotalDays : 0;

        return new PluginResult("abstinence",
        [
            new PluginMetricResult("abstinence_score", Math.Round(score, 1)),
            new PluginMetricResult("days_since_last", Math.Round(daysSinceLastEvent, 0))
        ]);
    }

    private async Task<PluginResult> CalculateExerciseAsync(PluginConfiguration config)
    {
        var now = DateTime.UtcNow;
        var cutoff42 = now.AddDays(-42);
        var cutoff7 = now.AddDays(-7);

        if (config.NumericFieldId == null)
            return new PluginResult("exercise", []);

        var fieldValues = await context.EventFieldValues
            .Where(fv => fv.EventFieldId == config.NumericFieldId &&
                         fv.EventRecord!.EventTypeId == config.EventTypeId &&
                         fv.EventRecord.RecordedAt >= cutoff42 &&
                         fv.NumberValue != null)
            .Include(fv => fv.EventRecord)
            .ToListAsync();

        var values42 = fieldValues.Select(fv => (double)fv.NumberValue!.Value).ToList();
        var values7 = fieldValues
            .Where(fv => fv.EventRecord!.RecordedAt >= cutoff7)
            .Select(fv => (double)fv.NumberValue!.Value)
            .ToList();

        var ctl = values42.Count > 0 ? values42.Average() : 0;
        var atl = values7.Count > 0 ? values7.Average() : 0;
        var tsb = ctl - atl;

        return new PluginResult("exercise",
        [
            new PluginMetricResult("fitness_ctl", Math.Round(ctl, 1)),
            new PluginMetricResult("fatigue_atl", Math.Round(atl, 1)),
            new PluginMetricResult("form_tsb", Math.Round(tsb, 1))
        ]);
    }

    private async Task<PluginResult> CalculateHabitStreakAsync(PluginConfiguration config)
    {
        var today = DateTime.UtcNow.Date;
        var dates = await context.EventRecords
            .Where(r => r.EventTypeId == config.EventTypeId)
            .Select(r => r.RecordedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync();

        // Current streak
        int currentStreak = 0;
        var checkDate = today;
        foreach (var date in dates)
        {
            if (date == checkDate || (date == today.AddDays(-1) && currentStreak == 0))
            {
                checkDate = date.AddDays(-1);
                currentStreak++;
            }
            else if (date == checkDate)
            {
                checkDate = checkDate.AddDays(-1);
                currentStreak++;
            }
            else
            {
                break;
            }
        }

        // Longest streak
        var allDates = dates.OrderBy(d => d).ToList();
        int longestStreak = 0, streak = 0;
        DateTime? prev = null;
        foreach (var date in allDates)
        {
            streak = prev.HasValue && date == prev.Value.AddDays(1) ? streak + 1 : 1;
            longestStreak = Math.Max(longestStreak, streak);
            prev = date;
        }

        var cutoff30 = today.AddDays(-29);
        var daysWithRecords30 = dates.Count(d => d >= cutoff30);
        var completionRate = (double)daysWithRecords30 / 30.0 * 100;

        return new PluginResult("habit_streak",
        [
            new PluginMetricResult("current_streak", currentStreak),
            new PluginMetricResult("longest_streak", longestStreak),
            new PluginMetricResult("completion_rate_30d", Math.Round(completionRate, 1))
        ]);
    }

    private async Task<PluginResult> CalculateMoodTrackerAsync(PluginConfiguration config)
    {
        if (config.NumericFieldId == null)
            return new PluginResult("mood_tracker", []);

        var now = DateTime.UtcNow;
        var cutoff30 = now.AddDays(-30);
        var cutoff7 = now.AddDays(-7);

        var fieldValues = await context.EventFieldValues
            .Where(fv => fv.EventFieldId == config.NumericFieldId &&
                         fv.EventRecord!.EventTypeId == config.EventTypeId &&
                         fv.NumberValue != null)
            .Include(fv => fv.EventRecord)
            .OrderBy(fv => fv.EventRecord!.RecordedAt)
            .ToListAsync();

        if (fieldValues.Count == 0)
            return new PluginResult("mood_tracker", []);

        const double alpha = 0.2;
        var ema = (double)fieldValues[0].NumberValue!.Value;
        foreach (var fv in fieldValues.Skip(1))
            ema = alpha * (double)fv.NumberValue!.Value + (1 - alpha) * ema;

        var values7 = fieldValues
            .Where(fv => fv.EventRecord!.RecordedAt >= cutoff7)
            .Select(fv => (double)fv.NumberValue!.Value).ToList();
        var values30 = fieldValues
            .Where(fv => fv.EventRecord!.RecordedAt >= cutoff30)
            .Select(fv => (double)fv.NumberValue!.Value).ToList();

        var avg7 = values7.Count > 0 ? values7.Average() : 0;
        var avg30 = values30.Count > 0 ? values30.Average() : 0;
        var stdDev30 = values30.Count > 1
            ? Math.Sqrt(values30.Sum(v => Math.Pow(v - values30.Average(), 2)) / (values30.Count - 1))
            : 0;

        return new PluginResult("mood_tracker",
        [
            new PluginMetricResult("mood_ema", Math.Round(ema, 2)),
            new PluginMetricResult("mood_avg_7d", Math.Round(avg7, 2)),
            new PluginMetricResult("mood_avg_30d", Math.Round(avg30, 2)),
            new PluginMetricResult("mood_volatility_30d", Math.Round(stdDev30, 2))
        ]);
    }

    private async Task<PluginResult> CalculateSleepTrackerAsync(PluginConfiguration config)
    {
        if (config.NumericFieldId == null)
            return new PluginResult("sleep_tracker", []);

        var cutoff30 = DateTime.UtcNow.AddDays(-30);
        var values = await context.EventFieldValues
            .Where(fv => fv.EventFieldId == config.NumericFieldId &&
                         fv.EventRecord!.EventTypeId == config.EventTypeId &&
                         fv.EventRecord.RecordedAt >= cutoff30 &&
                         fv.NumberValue != null)
            .Select(fv => (double)fv.NumberValue!.Value)
            .ToListAsync();

        if (values.Count == 0)
            return new PluginResult("sleep_tracker", []);

        var avg = values.Average();
        var stdDev = values.Count > 1
            ? Math.Sqrt(values.Sum(v => Math.Pow(v - avg, 2)) / (values.Count - 1))
            : 0;

        return new PluginResult("sleep_tracker",
        [
            new PluginMetricResult("avg_sleep_30d", Math.Round(avg, 2)),
            new PluginMetricResult("bedtime_stability", Math.Round(stdDev, 2))
        ]);
    }

    private async Task<PluginResult> CalculateWeightTrendAsync(PluginConfiguration config)
    {
        if (config.NumericFieldId == null)
            return new PluginResult("weight_trend", []);

        var now = DateTime.UtcNow;
        var cutoff30 = now.AddDays(-30);
        var cutoff7 = now.AddDays(-7);

        var fieldValues = await context.EventFieldValues
            .Where(fv => fv.EventFieldId == config.NumericFieldId &&
                         fv.EventRecord!.EventTypeId == config.EventTypeId &&
                         fv.NumberValue != null)
            .Include(fv => fv.EventRecord)
            .OrderByDescending(fv => fv.EventRecord!.RecordedAt)
            .ToListAsync();

        if (fieldValues.Count == 0)
            return new PluginResult("weight_trend", []);

        var latest = (double)fieldValues[0].NumberValue!.Value;
        var values7 = fieldValues.Where(fv => fv.EventRecord!.RecordedAt >= cutoff7)
            .Select(fv => (double)fv.NumberValue!.Value).ToList();
        var values30 = fieldValues.Where(fv => fv.EventRecord!.RecordedAt >= cutoff30)
            .Select(fv => (double)fv.NumberValue!.Value).ToList();

        var avg7 = values7.Count > 0 ? values7.Average() : latest;
        var avg30 = values30.Count > 0 ? values30.Average() : latest;

        double weeklyTrend = 0;
        var points = fieldValues
            .Where(fv => fv.EventRecord!.RecordedAt >= cutoff30)
            .Select(fv => ((fv.EventRecord!.RecordedAt - cutoff30).TotalDays, (double)fv.NumberValue!.Value))
            .ToList();

        if (points.Count >= 2)
        {
            var n = points.Count;
            var sumX = points.Sum(p => p.Item1);
            var sumY = points.Sum(p => p.Item2);
            var sumXY = points.Sum(p => p.Item1 * p.Item2);
            var sumX2 = points.Sum(p => p.Item1 * p.Item1);
            var denominator = n * sumX2 - sumX * sumX;
            if (Math.Abs(denominator) > 1e-10)
                weeklyTrend = (n * sumXY - sumX * sumY) / denominator * 7;
        }

        return new PluginResult("weight_trend",
        [
            new PluginMetricResult("latest_weight", Math.Round(latest, 2)),
            new PluginMetricResult("avg_weight_7d", Math.Round(avg7, 2)),
            new PluginMetricResult("avg_weight_30d", Math.Round(avg30, 2)),
            new PluginMetricResult("weekly_trend", Math.Round(weeklyTrend, 3))
        ]);
    }
}
