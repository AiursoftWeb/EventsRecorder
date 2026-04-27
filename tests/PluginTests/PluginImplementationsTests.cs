using Aiursoft.EventsRecorder.Entities;
using Aiursoft.EventsRecorder.Services.Plugins.Implementations;

namespace Aiursoft.EventsRecorder.Tests.PluginTests;

[TestClass]
public class PluginImplementationsTests
{
    private readonly DateTime _now = new(2026, 4, 27);

    [TestMethod]
    public async Task AbstinencePlugin_BasicTest()
    {
        var plugin = new AbstinencePlugin();
        var config = new Dictionary<string, string> { ["event_type_ids"] = "1" };
        var eventTypes = new List<EventType>
        {
            new()
            {
                Id = 1,
                Name = "Smoking",
                UserId = "user-1",
                Records =
                [
                    new EventRecord { RecordedAt = _now.AddDays(-10), UserId = "user-1" }, // First relapse
                    new EventRecord { RecordedAt = _now.AddDays(-5), UserId = "user-1" }   // Second relapse
                ]
            }
        };

        var results = await plugin.ComputeAsync(config, eventTypes, _now);

        var score = results.First(r => r.MetricId == "score").Value;
        var daysSinceLast = results.First(r => r.MetricId == "days_since_last").Value;

        // Start score = 0
        // Day -10: score = min(0+10, 100) = 10. Then halved = 5.
        // Day -9: 15
        // Day -8: 25
        // Day -7: 35
        // Day -6: 45
        // Day -5: 55. Then halved = 27.5.
        // Day -4: 37.5
        // Day -3: 47.5
        // Day -2: 57.5
        // Day -1: 67.5
        // Day 0: 77.5
        Assert.AreEqual(77.5, score);
        Assert.AreEqual(5, daysSinceLast);
    }

    [TestMethod]
    public async Task AbstinencePlugin_EmptyConfigTest()
    {
        var plugin = new AbstinencePlugin();
        var config = new Dictionary<string, string>();
        var results = await plugin.ComputeAsync(config, [], _now);
        Assert.AreEqual(0, results.Count);

        config["event_type_ids"] = "";
        results = await plugin.ComputeAsync(config, [], _now);
        Assert.AreEqual(0, results.Count);

        config["event_type_ids"] = "abc"; // Invalid IDs
        results = await plugin.ComputeAsync(config, [], _now);
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task HabitStreakPlugin_BasicTest()
    {
        var plugin = new HabitStreakPlugin();
        var config = new Dictionary<string, string> { ["event_type_ids"] = "1" };
        var eventTypes = new List<EventType>
        {
            new()
            {
                Id = 1,
                Name = "Habit",
                UserId = "user-1",
                Records =
                [
                    new EventRecord { RecordedAt = _now.AddDays(-3), UserId = "user-1" },
                    new EventRecord { RecordedAt = _now.AddDays(-2), UserId = "user-1" },
                    new EventRecord { RecordedAt = _now.AddDays(-1), UserId = "user-1" }
                    // Today (0) missing
                ]
            }
        };

        var results = await plugin.ComputeAsync(config, eventTypes, _now);

        Assert.AreEqual(3.0, results.First(r => r.MetricId == "current_streak").Value);
        Assert.AreEqual(3.0, results.First(r => r.MetricId == "longest_streak").Value);
        Assert.AreEqual(Math.Round(3.0 / 30.0 * 100, 1), results.First(r => r.MetricId == "completion_rate_30d").Value);
    }

    [TestMethod]
    public async Task HabitStreakPlugin_EmptyConfigTest()
    {
        var plugin = new HabitStreakPlugin();
        var config = new Dictionary<string, string>();
        var results = await plugin.ComputeAsync(config, [], _now);
        Assert.AreEqual(0, results.Count);

        config["event_type_ids"] = "";
        results = await plugin.ComputeAsync(config, [], _now);
        Assert.AreEqual(0, results.Count);

        config["event_type_ids"] = "abc"; // Invalid IDs
        results = await plugin.ComputeAsync(config, [], _now);
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public async Task WeightTrendPlugin_BasicTest()
    {
        var plugin = new WeightTrendPlugin();
        var config = new Dictionary<string, string>
        {
            ["event_type_id"] = "1",
            ["weight_field_id"] = "10"
        };

        var eventTypes = new List<EventType>
        {
            new()
            {
                Id = 1,
                Name = "Weight",
                UserId = "user-1",
                Records =
                [
                    new EventRecord
                    {
                        RecordedAt = _now.AddDays(-10),
                        UserId = "user-1",
                        FieldValues = [new EventFieldValue { EventFieldId = 10, NumberValue = 80 }]
                    },
                    new EventRecord
                    {
                        RecordedAt = _now.AddDays(-3),
                        UserId = "user-1",
                        FieldValues = [new EventFieldValue { EventFieldId = 10, NumberValue = 79 }]
                    }
                ]
            }
        };

        var results = await plugin.ComputeAsync(config, eventTypes, _now);

        var latest = results.First(r => r.MetricId == "latest").Value;
        var trend = results.First(r => r.MetricId == "trend").Value;

        Assert.AreEqual(79.0, latest);
        // Slope = (79 - 80) / (-3 - (-10)) = -1 / 7 = -0.142857 kg/day
        // Trend = Slope * 7 = -1.0 kg/week
        Assert.AreEqual(-1.0, trend);
    }

    [TestMethod]
    public async Task MoodTrackerPlugin_BasicTest()
    {
        var plugin = new MoodTrackerPlugin();
        var config = new Dictionary<string, string>
        {
            ["event_type_id"] = "1",
            ["mood_field_id"] = "10"
        };

        var eventTypes = new List<EventType>
        {
            new()
            {
                Id = 1,
                Name = "Mood",
                UserId = "user-1",
                Records =
                [
                    new EventRecord
                    {
                        RecordedAt = _now.AddDays(-2),
                        UserId = "user-1",
                        FieldValues = [new EventFieldValue { EventFieldId = 10, NumberValue = 5 }]
                    },
                    new EventRecord
                    {
                        RecordedAt = _now.AddDays(-1),
                        UserId = "user-1",
                        FieldValues = [new EventFieldValue { EventFieldId = 10, NumberValue = 10 }]
                    }
                ]
            }
        };

        var results = await plugin.ComputeAsync(config, eventTypes, _now);

        var ema = results.First(r => r.MetricId == "ema").Value;
        // alpha = 0.2
        // Start ema = 5
        // Next: 0.2 * 10 + (1 - 0.2) * 5 = 2 + 4 = 6
        Assert.AreEqual(6.0, ema);
    }

    [TestMethod]
    public async Task ExercisePlugin_BasicTest()
    {
        var plugin = new ExercisePlugin();
        var config = new Dictionary<string, string>
        {
            ["source_event_types"] = "1",
            ["calories_fields"] = "1:10"
        };

        var eventTypes = new List<EventType>
        {
            new()
            {
                Id = 1,
                Name = "Run",
                UserId = "user-1",
                Records =
                [
                    new EventRecord
                    {
                        RecordedAt = _now.AddDays(-2),
                        UserId = "user-1",
                        FieldValues = [new EventFieldValue { EventFieldId = 10, NumberValue = 500 }]
                    },
                    new EventRecord
                    {
                        RecordedAt = _now.AddDays(-1),
                        UserId = "user-1",
                        FieldValues = [new EventFieldValue { EventFieldId = 10, NumberValue = 1000 }]
                    }
                ]
            }
        };

        var results = await plugin.ComputeAsync(config, eventTypes, _now);

        var fitness = results.First(r => r.MetricId == "fitness").Value;
        var fatigue = results.First(r => r.MetricId == "fatigue").Value;

        // Simple average: (500 + 1000) / 2 = 750
        Assert.AreEqual(750.0, fitness);
        Assert.AreEqual(750.0, fatigue);
    }
}
