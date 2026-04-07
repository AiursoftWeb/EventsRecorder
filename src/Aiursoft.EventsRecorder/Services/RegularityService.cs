using Aiursoft.Scanner.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aiursoft.EventsRecorder.Services;

public class RegularityService : ISingletonDependency
{
    public double CalculateScore(IEnumerable<DateTime> timestamps)
    {
        var list = timestamps.OrderBy(t => t).ToList();
        if (list.Count < 3) // Need at least some data to calculate regularity
        {
            return 0;
        }

        // Only take the last 8
        if (list.Count > 8)
        {
            list = list.Skip(list.Count - 8).ToList();
        }

        var n = list.Count;

        // 1. Interval Stability Score (S_int)
        var deltas = new List<double>();
        for (var i = 0; i < n - 1; i++)
        {
            deltas.Add((list[i + 1] - list[i]).TotalHours);
        }

        var mDelta = Median(deltas);
        if (mDelta <= 0) return 0;

        var d = deltas.Select(delta => Math.Abs(delta - mDelta)).OrderBy(v => v).ToList();
        
        // Discard the 2 largest deviations if we have enough deltas
        var trimmedD = d.Count > 2 ? d.Take(d.Count - 2).ToList() : d;
        var madTrimmed = trimmedD.Average();

        const double k1 = 2.0;
        var sInt = Math.Max(0, 100 * (1 - k1 * (madTrimmed / mDelta)));

        // 2. Clock-Time Stability Score (S_time)
        var xList = new List<double>();
        var yList = new List<double>();
        foreach (var t in list)
        {
            var hour = t.Hour + t.Minute / 60.0 + t.Second / 3600.0;
            var angle = hour * 2.0 * Math.PI / 24.0;
            xList.Add(Math.Cos(angle));
            yList.Add(Math.Sin(angle));
        }

        var avgX = xList.Average();
        var avgY = yList.Average();
        var hMeanAngle = Math.Atan2(avgY, avgX);
        var hMean = (hMeanAngle * 24.0 / (2.0 * Math.PI) + 24.0) % 24.0;

        var distances = new List<double>();
        foreach (var t in list)
        {
            var hour = t.Hour + t.Minute / 60.0 + t.Second / 3600.0;
            var diff = Math.Abs(hour - hMean);
            var dist = Math.Min(diff, 24.0 - diff);
            distances.Add(dist);
        }

        var sortedDistances = distances.OrderBy(v => v).ToList();
        // Discard 2 largest distances if we have enough data
        var trimmedDistances = sortedDistances.Count > 2 ? sortedDistances.Take(sortedDistances.Count - 2).ToList() : sortedDistances;
        var timeDev = trimmedDistances.Average();

        var sTime = Math.Max(0, 100 * (1 - timeDev / 4.0));

        // 3. Dynamic Blending
        var alpha = Math.Max(0, Math.Min(1, (72.0 - mDelta) / 48.0));
        var finalScore = alpha * sTime + (1 - alpha) * sInt;

        return finalScore;
    }

    private double Median(IEnumerable<double> source)
    {
        var sortedList = source.OrderBy(v => v).ToList();
        if (sortedList.Count == 0) return 0;
        var count = sortedList.Count;
        if (count % 2 == 0)
        {
            return (sortedList[count / 2 - 1] + sortedList[count / 2]) / 2.0;
        }
        return sortedList[count / 2];
    }
}
