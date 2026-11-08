using System.Diagnostics;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Core.Storage;

internal static class Lab07Demo
{
    private const int LowIntervalThresholdMs = 1000;
    private const int DefaultTopN = 5;

    public static void Run(string[] args)
    {
        var command = args.FirstOrDefault(a =>
            a.StartsWith("report-", StringComparison.OrdinalIgnoreCase)) ?? "report-top";
        bool file = args.Contains("--file", StringComparer.OrdinalIgnoreCase);
        bool cache = args.Contains("--cache", StringComparer.OrdinalIgnoreCase);
        bool factory = args.Contains("--factory", StringComparer.OrdinalIgnoreCase);
        string dataPath = ProjectPaths.GetSensorDataPath(args);

        ISensorStore store = factory
            ? StoreFactory.Create(args)
            : file
                ? new FileSensorStore(dataPath)
                : new InMemorySensorStore(SampleData.Sensors());

        // The Lab 5 convention: seed an empty JSON store, never overwrite existing data.
        if (file && store.List().Count == 0)
        {
            foreach (var sensor in SampleData.Sensors()) store.Add(sensor);
        }
        if (cache) store = new CachingSensorStore(store);

        var service = new SensorService(store);
        var items = service.All(); // Snapshot from store; reports consume IEnumerable<Sensor>.
        Console.WriteLine($"=== LAB07: {command} ===");
        Console.WriteLine($"Джерело: {(file ? "FileSensorStore" : "InMemorySensorStore")}; сенсорів: {items.Count}");

        switch (command.ToLowerInvariant())
        {
            case "report-top":
                Console.WriteLine("ТОП-5 за найбільшим інтервалом опитування (мс):");
                PrintRows(SensorReportService.TopByInterval(items, DefaultTopN));
                break;
            case "report-low":
                Console.WriteLine($"Часте опитування: інтервал < {LowIntervalThresholdMs} мс:");
                PrintRows(SensorReportService.LowInterval(items, LowIntervalThresholdMs));
                break;
            case "report-groups":
                PrintGroups(SensorReportService.ByModel(items));
                break;
            case "report-sum":
                var total = SensorReportService.Totals(items);
                Console.WriteLine($"LAB07: totals items={total.Items}, total={total.Total} ms, " +
                    $"average={total.Average:F2} ms, max={total.Max} ms");
                break;
            case "report-deferred":
                DemonstrateDeferredExecution(items);
                break;
            case "report-join":
                DemonstrateGroupJoin(items);
                break;
            case "report-top-readings":
                var summaries = SensorReportService.ReadingsBySensor(items, DemoReadings());
                Console.WriteLine("ТОП-5 сенсорів за кількістю демонстраційних вимірювань:");
                PrintReadingRows(SensorReportService.TopByReadings(summaries, DefaultTopN));
                break;
            case "report-query":
                var method = SensorReportService.ByModel(items);
                var syntax = SensorReportService.ByModelQuerySyntax(items);
                Console.WriteLine("Групування у синтаксисі from/group/into:");
                PrintGroups(syntax);
                Console.WriteLine($"LAB07: query syntax equals method syntax = {method.SequenceEqual(syntax)}");
                break;
            case "report-bench":
                DemonstrateBenchmark();
                break;
            case "report-selftest":
                Lab07SelfTests.Run();
                break;
            default:
                Console.Error.WriteLine("Невідома команда. Використовуй: report-top | report-low | " +
                    "report-groups | report-sum | report-deferred | report-join | " +
                    "report-top-readings | report-query | report-bench | report-selftest");
                Environment.ExitCode = 1;
                break;
        }
    }

    private static void PrintRows(IReadOnlyList<SensorReportLine> lines)
    {
        Console.WriteLine("{0,-14} {1,-12} {2,-14} {3,7} {4,-12}",
            "ID", "Модель", "Контролер", "мс", "Статус");
        foreach (var line in lines)
            Console.WriteLine($"{line.Id,-14} {line.Model,-12} {line.ControllerId,-14} " +
                $"{line.PingIntervalMs,7} {line.Status,-12}");
        Console.WriteLine($"LAB07: rows={lines.Count}");
    }

    private static void PrintGroups(IReadOnlyList<SensorGroupSummary> groups)
    {
        Console.WriteLine("{0,-12} {1,10} {2,22}",
            "Модель", "Позицій", "Сума інтервалів, мс");
        foreach (var group in groups)
            Console.WriteLine($"{group.Key,-12} {group.Count,10} {group.TotalPingIntervalMs,22}");
        Console.WriteLine($"LAB07: groups={groups.Count}");
    }

    // A deferred query sees an addition made before its second enumeration.
    // Once ToList materializes it, clearing the source cannot change the snapshot.
    private static void DemonstrateDeferredExecution(IReadOnlyList<Sensor> items)
    {
        var source = items.ToList();
        var query = source.Where(s => s.PingIntervalMs < LowIntervalThresholdMs);
        int before = query.Count();
        source.Add(Sensor.Create("S-LAB07-TEMP", "ESP-TEMP", "DHT22", 450));
        int after = query.Count();
        var snapshot = query.ToList();
        source.Clear();
        Console.WriteLine($"LAB07: deferred before={before} after={after} " +
            $"snapshot={snapshot.Count} after-clear={snapshot.Count}");
        Console.WriteLine("Where відкладає виконання до Count(); ToList() зберігає знімок.");
    }

    // Demonstration data, not real persisted measurements: task requires a second sequence.
    private static IReadOnlyList<SensorReading> DemoReadings() =>
    [
        new("S-001", 120), new("S-001", 110), new("S-001", 115),
        new("S-002", 90), new("S-002", 95),
        new("S-003", 200),
        new("S-004", 130), new("S-004", 125),
        new("S-006", 105),
        new("S-999", 80) // no matching sensor: correctly excluded by GroupJoin
    ];

    private static void DemonstrateGroupJoin(IReadOnlyList<Sensor> sensors)
    {
        var summaries = SensorReportService.ReadingsBySensor(sensors, DemoReadings());
        Console.WriteLine("GroupJoin: сенсори + демонстраційні вимірювання (у т.ч. без вимірювань):");
        PrintReadingRows(summaries.Take(8).ToList());
        Console.WriteLine($"LAB07: join rows={summaries.Count}, " +
            $"without readings={summaries.Count(row => row.ReadingCount == 0)}");
    }

    private static void PrintReadingRows(IReadOnlyList<SensorReadingSummary> rows)
    {
        Console.WriteLine("{0,-14} {1,-12} {2,12} {3,15}",
            "ID", "Модель", "Вимірювань", "Сума часу, мс");
        foreach (var row in rows)
            Console.WriteLine($"{row.SensorId,-14} {row.Model,-12} " +
                $"{row.ReadingCount,12} {row.TotalResponseTimeMs,15}");
    }

    // Optional task #4: equal queries on 100,000 sensors, differing only in a ToList mid-chain.
    private static void DemonstrateBenchmark()
    {
        var models = new[] { "DHT22", "HC-SR04", "BMP280", "PIR" };
        var source = Enumerable.Range(1, 100_000)
            .Select(i => Sensor.Create($"S-B{i:000000}", $"ESP-{i % 20:00}",
                models[i % models.Length], 500 + i % 10_000))
            .ToList();

        static IReadOnlyList<SensorReportLine> Execute(
            IEnumerable<Sensor> source, bool intermediateToList)
        {
            IEnumerable<Sensor> filtered = source.Where(s => s.PingIntervalMs < 8_000);
            if (intermediateToList) filtered = filtered.ToList();
            return filtered.OrderByDescending(s => s.PingIntervalMs)
                .ThenBy(s => s.Id, StringComparer.Ordinal)
                .Take(100)
                .Select(s => new SensorReportLine(s.Id, s.Model, s.ControllerId,
                    s.PingIntervalMs, s.Status.ToString()))
                .ToList();
        }

        _ = Execute(source, false); // JIT warmup for fairer timing
        _ = Execute(source, true);

        static (double Milliseconds, IReadOnlyList<SensorReportLine> Lines) Measure(
            List<Sensor> items, bool intermediateToList)
        {
            var watch = Stopwatch.StartNew();
            IReadOnlyList<SensorReportLine> rows = Array.Empty<SensorReportLine>();
            for (int round = 0; round < 3; round++) rows = Execute(items, intermediateToList);
            watch.Stop();
            return (watch.Elapsed.TotalMilliseconds / 3d, rows);
        }

        var without = Measure(source, false);
        var with = Measure(source, true);
        bool sameResults = without.Lines.SequenceEqual(with.Lines);
        Console.WriteLine("LAB07: benchmark records=100000, 3 rounds per variant");
        Console.WriteLine($"Without middle ToList: {without.Milliseconds:F2} ms");
        Console.WriteLine($"With middle ToList:    {with.Milliseconds:F2} ms");
        Console.WriteLine($"LAB07: benchmark same-results={sameResults}");
        Console.WriteLine("Час залежить від ПК і навантаження; проміжний ToList створює додатковий список.");
        if (!sameResults) throw new InvalidOperationException("Benchmark outputs differ.");
    }
}
