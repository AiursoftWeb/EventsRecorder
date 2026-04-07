using Aiursoft.EventsRecorder.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Aiursoft.EventsRecorder.Tests;

[TestClass]
public class RegularityServiceTests
{
    private RegularityService _service = new();

    [TestMethod]
    public void TestSmallSample()
    {
        var timestamps = new List<DateTime>
        {
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1)
        };
        var score = _service.CalculateScore(timestamps);
        Assert.AreEqual(0, score);
    }

    [TestMethod]
    public void TestPerfectDaily()
    {
        var timestamps = new List<DateTime>();
        var baseTime = new DateTime(2023, 1, 1, 8, 0, 0); // 8:00 AM
        for (int i = 0; i < 8; i++)
        {
            timestamps.Add(baseTime.AddDays(i));
        }
        var score = _service.CalculateScore(timestamps);
        Assert.IsTrue(score > 99);
    }

    [TestMethod]
    public void TestPerfectWeekly()
    {
        var timestamps = new List<DateTime>();
        var baseTime = new DateTime(2023, 1, 1, 8, 0, 0); 
        for (int i = 0; i < 8; i++)
        {
            timestamps.Add(baseTime.AddDays(i * 7));
        }
        var score = _service.CalculateScore(timestamps);
        // Weekly (168h) -> alpha should be 0 (72 - 168 / 48 = negative -> capped at 0)
        // Interval stability should be high.
        Assert.IsTrue(score > 99);
    }

    [TestMethod]
    public void TestOneAnomaly()
    {
        var timestamps = new List<DateTime>();
        var baseTime = new DateTime(2023, 1, 1, 8, 0, 0);
        for (int i = 0; i < 8; i++)
        {
            if (i == 4)
            {
                timestamps.Add(baseTime.AddDays(i).AddHours(5)); // One anomaly
            }
            else
            {
                timestamps.Add(baseTime.AddDays(i));
            }
        }
        var score = _service.CalculateScore(timestamps);
        // Should still be high because we discard 2 largest deviations
        Assert.IsTrue(score > 85);
    }

    [TestMethod]
    public void TestTotalChaos()
    {
        var timestamps = new List<DateTime>
        {
            new(2023, 1, 1, 8, 0, 0),
            new(2023, 1, 2, 12, 0, 0),
            new(2023, 1, 5, 20, 0, 0),
            new(2023, 1, 6, 2, 0, 0),
            new(2023, 1, 10, 15, 0, 0),
            new(2023, 1, 12, 9, 0, 0),
            new(2023, 1, 13, 22, 0, 0),
            new(2023, 1, 15, 5, 0, 0)
        };
        var score = _service.CalculateScore(timestamps);
        Assert.IsTrue(score < 50);
    }
}
