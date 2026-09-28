using Aiursoft.EventsRecorder.Entities;
using Aiursoft.EventsRecorder.Services.Plugins.Implementations;

namespace Aiursoft.EventsRecorder.Tests.PluginTests;

[TestClass]
public class GeneralPluginTests
{
    [TestMethod]
    public async Task WeeklyReview_UsesMondayWeekBoundariesAndDistinctActiveDays()
    {
        var now = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
        var type = CreateType(1);
        type.Records =
        [
            Record(new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Utc)),
            Record(new DateTime(2026, 9, 27, 8, 0, 0, DateTimeKind.Utc)),
            Record(new DateTime(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc)),
            Record(new DateTime(2026, 9, 28, 9, 0, 0, DateTimeKind.Utc)),
            Record(new DateTime(2026, 9, 30, 8, 0, 0, DateTimeKind.Utc)),
            Record(now.AddHours(1))
        ];

        var result = await new WeeklyReviewPlugin().ComputeAsync(
            new Dictionary<string, string> { ["event_type_ids"] = "1" }, [type], now);

        Assert.AreEqual(3, result.Single(m => m.MetricId == "this_week").Value);
        Assert.AreEqual(2, result.Single(m => m.MetricId == "last_week").Value);
        Assert.AreEqual(2, result.Single(m => m.MetricId == "active_days").Value);
        Assert.AreEqual(1, result.Single(m => m.MetricId == "change").Value);
    }

    [TestMethod]
    public async Task TimeInvestment_SumsChosenDurationFieldsAcrossTypes()
    {
        var now = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
        var first = CreateType(1);
        first.Records =
        [
            Record(now.AddDays(-1), new EventFieldValue { EventFieldId = 11, TimespanTicks = TimeSpan.FromHours(2).Ticks }),
            Record(now.AddDays(-8), new EventFieldValue { EventFieldId = 11, TimespanTicks = TimeSpan.FromHours(3).Ticks })
        ];
        var second = CreateType(2);
        second.Records =
        [
            Record(now.AddDays(-2), new EventFieldValue { EventFieldId = 22, TimespanTicks = TimeSpan.FromMinutes(30).Ticks }),
            Record(now.AddDays(-2), new EventFieldValue { EventFieldId = 99, TimespanTicks = TimeSpan.FromHours(8).Ticks })
        ];

        var result = await new TimeInvestmentPlugin().ComputeAsync(
            new Dictionary<string, string> { ["event_type_ids"] = "1,2", ["duration_fields"] = "1:11,2:22" },
            [first, second], now);

        Assert.AreEqual(2.5, result.Single(m => m.MetricId == "this_week").Value);
        Assert.AreEqual(3, result.Single(m => m.MetricId == "last_week").Value);
        Assert.AreEqual(5.5, result.Single(m => m.MetricId == "last_30_days").Value);
    }

    [TestMethod]
    public async Task PersonalBest_UsesLastOccurrenceOfTheMaximum()
    {
        var now = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
        var type = CreateType(1, new EventField { Id = 7, Name = "Score", FieldType = FieldType.Number });
        type.Records =
        [
            Record(now.AddDays(-10), new EventFieldValue { EventFieldId = 7, NumberValue = 100 }),
            Record(now.AddDays(-3), new EventFieldValue { EventFieldId = 7, NumberValue = 100 }),
            Record(now.AddDays(-1), new EventFieldValue { EventFieldId = 7, NumberValue = 90 })
        ];

        var result = await new PersonalBestPlugin().ComputeAsync(
            new Dictionary<string, string> { ["event_type_id"] = "1", ["value_field_id"] = "7" }, [type], now);

        Assert.AreEqual(100, result.Single(m => m.MetricId == "best").Value);
        Assert.AreEqual(90, result.Single(m => m.MetricId == "latest").Value);
        Assert.AreEqual(10, result.Single(m => m.MetricId == "gap").Value);
        Assert.AreEqual(3, result.Single(m => m.MetricId == "days_since_best").Value);
    }

    [TestMethod]
    public async Task WeeklyGoal_CountsOneDayOnceAndRejectsInvalidTargets()
    {
        var now = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
        var type = CreateType(1);
        type.Records = [Record(now.AddDays(-1)), Record(now.AddDays(-1).AddHours(1)), Record(now)];
        var plugin = new WeeklyGoalPlugin();
        var config = new Dictionary<string, string> { ["event_type_ids"] = "1", ["target_days"] = "3" };

        var result = await plugin.ComputeAsync(config, [type], now);

        Assert.AreEqual(2, result.Single(m => m.MetricId == "active_days").Value);
        Assert.AreEqual(1, result.Single(m => m.MetricId == "remaining_days").Value);
        Assert.AreEqual(66.7, result.Single(m => m.MetricId == "progress").Value);

        config["target_days"] = "8";
        Assert.AreEqual(0, (await plugin.ComputeAsync(config, [type], now)).Count);
    }

    [TestMethod]
    public async Task RecordingRhythm_MeasuresIntervalsRatherThanCalendarDays()
    {
        var now = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
        var type = CreateType(1);
        type.Records = [Record(now.AddDays(-4)), Record(now.AddDays(-3)), Record(now.AddDays(-1))];

        var result = await new RecordingRhythmPlugin().ComputeAsync(
            new Dictionary<string, string> { ["event_type_ids"] = "1" }, [type], now);

        Assert.AreEqual(36, result.Single(m => m.MetricId == "median_gap").Value);
        Assert.AreEqual(48, result.Single(m => m.MetricId == "latest_gap").Value);
        Assert.AreEqual(33.3, result.Single(m => m.MetricId == "variation").Value);
        Assert.AreEqual(24, result.Single(m => m.MetricId == "since_last").Value);
    }

    [TestMethod]
    public async Task EventAssociation_UsesOnlyTriggersInThePrevious24Hours()
    {
        var now = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
        var trigger = CreateType(1);
        var outcome = CreateType(2, new EventField { Id = 7, Name = "Result", FieldType = FieldType.Number });
        for (var i = 0; i < 6; i++)
        {
            var time = now.AddDays(-12 + i * 2);
            if (i < 3)
                trigger.Records.Add(Record(time.AddHours(-1)));
            outcome.Records.Add(Record(time,
                new EventFieldValue { EventFieldId = 7, NumberValue = i < 3 ? 10 : 4 }));
        }

        var result = await new EventAssociationPlugin().ComputeAsync(
            new Dictionary<string, string>
            {
                ["trigger_event_type_id"] = "1", ["outcome_event_type_id"] = "2", ["outcome_field_id"] = "7"
            }, [trigger, outcome], now);

        Assert.AreEqual(3, result.Single(m => m.MetricId == "with_count").Value);
        Assert.AreEqual(3, result.Single(m => m.MetricId == "without_count").Value);
        Assert.AreEqual(10, result.Single(m => m.MetricId == "with_average").Value);
        Assert.AreEqual(4, result.Single(m => m.MetricId == "without_average").Value);
        Assert.AreEqual(6, result.Single(m => m.MetricId == "difference").Value);
    }

    private static EventType CreateType(int id, params EventField[] fields) => new()
    {
        Id = id,
        Name = $"Type {id}",
        UserId = "user-1",
        Fields = fields.ToList()
    };

    private static EventRecord Record(DateTime at, params EventFieldValue[] values) => new()
    {
        RecordedAt = at,
        UserId = "user-1",
        FieldValues = values.ToList()
    };
}
