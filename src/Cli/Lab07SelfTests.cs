using Core;
using Core.Domain;
using Core.Reports;

internal static class Lab07SelfTests
{
    public static void Run()
    {
        var sensors = SampleData.Sensors().ToList();
        Require(sensors.Count >= 15 && sensors.Count <= 20, "15-20 sample records");
        Require(sensors.Select(s => s.Model).Distinct().Count() >= 4, "4 sample models");

        var low = SensorReportService.LowInterval(sensors, 1000);
        Require(low.Count >= 2 && low.Count <= 3, "2-3 low-interval sensors");
        Require(low.All(s => s.PingIntervalMs < 1000), "Where predicate");
        Require(low.SequenceEqual(low.OrderBy(s => s.PingIntervalMs)
            .ThenBy(s => s.Id, StringComparer.Ordinal)), "LowInterval order");

        var top = SensorReportService.TopByInterval(sensors, 5);
        var reverseTop = SensorReportService.TopByInterval(sensors.AsEnumerable().Reverse(), 5);
        Require(top.Count == 5 && top.SequenceEqual(reverseTop), "deterministic Top-5");
        Require(SensorReportService.TopByInterval(sensors, 0).Count == 0, "Top-0 is empty");

        var groups = SensorReportService.ByModel(sensors);
        Require(groups.Count >= 4 && groups.Sum(g => g.Count) == sensors.Count, "GroupBy Count");
        var totals = SensorReportService.Totals(sensors);
        Require(groups.Sum(g => g.TotalPingIntervalMs) == totals.Total, "GroupBy Sum");
        Require(totals.Items == sensors.Count && totals.Average > 0 && totals.Max > 0, "aggregates");
        Require(groups.SequenceEqual(SensorReportService.ByModelQuerySyntax(sensors)),
            "query syntax equals method syntax");

        Require(SensorReportService.LowInterval(Array.Empty<Sensor>(), 1000).Count == 0,
            "empty LowInterval");
        Require(SensorReportService.TopByInterval(Array.Empty<Sensor>(), 5).Count == 0,
            "empty TopByInterval");
        Require(SensorReportService.ByModel(Array.Empty<Sensor>()).Count == 0,
            "empty GroupBy");
        Require(SensorReportService.Totals(Array.Empty<Sensor>()) == (0, 0, 0d, 0),
            "empty Totals");

        var readings = new[]
        {
            new SensorReading("S-001", 20),
            new SensorReading("S-001", 30),
            new SensorReading("S-002", 50),
            new SensorReading("S-UNKNOWN", 99)
        };
        var joined = SensorReportService.ReadingsBySensor(sensors, readings);
        Require(joined.Count == sensors.Count, "GroupJoin retains every sensor");
        Require(joined.Single(x => x.SensorId == "S-001").ReadingCount == 2,
            "GroupJoin counts readings");
        Require(joined.Any(x => x.ReadingCount == 0), "GroupJoin retains unmatched sensors");
        Require(SensorReportService.TopByReadings(joined, 5).Count == 5,
            "second TopBy<T> usage");

        var mutable = sensors.ToList();
        var query = mutable.Where(x => x.PingIntervalMs < 1000);
        int before = query.Count();
        mutable.Add(Sensor.Create("S-DEFER-TEST", "ESP-TEMP", "PIR", 450));
        Require(query.Count() == before + 1, "deferred query sees modification");
        var snapshot = query.ToList();
        mutable.Clear();
        Require(snapshot.Count == before + 1, "materialized snapshot is stable");

        Console.WriteLine("LAB07: all assertions passed");
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException($"LAB07 selftest FAILED: {label}");
        Console.WriteLine($"[OK] {label}");
    }
}
