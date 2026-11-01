using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Import;
using Core.Services;
using Core.Storage;

internal static class Lab06Demo
{
    public static void Run(string[] args)
    {
        bool useFile = args.Contains("--file", StringComparer.OrdinalIgnoreCase);
        bool useCache = args.Contains("--cache", StringComparer.OrdinalIgnoreCase);
        bool useFactory = args.Contains("--factory", StringComparer.OrdinalIgnoreCase);
        bool useSinks = args.Contains("--sinks", StringComparer.OrdinalIgnoreCase);

        string dataPath = ProjectPaths.GetSensorDataPath(args);
        ISensorStore store = useFactory
            ? StoreFactory.Create(args)
            : useFile
                ? new FileSensorStore(dataPath)
                : new InMemorySensorStore(SampleData.Sensors());

        if (useFile && store.List().Count == 0)
            foreach (var sensor in SampleData.Sensors()) store.Add(sensor);

        if (useCache)
            store = new CachingSensorStore(store);

        string root = FindRepositoryRoot();
        string logPath = Path.Combine(root, "logs", "app.log");
        string sinkLogPath = Path.Combine(root, "logs", "sink.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        // Reset only demo logs, so this run is easy to verify.
        File.WriteAllText(logPath, string.Empty);
        INotificationSink? notificationSink = null;
        if (useSinks)
        {
            File.WriteAllText(sinkLogPath, string.Empty);
            notificationSink = new CompositeNotificationSink(
                new ConsoleSink(), new FileSink(sinkLogPath));
        }

        // Manual DI: the service does not know the console/file implementations.
        var service = new SensorService(store, notificationSink);
        int changes = 0;
        int loggedLines = 0;

        void OnSensorChanged(object? sender, SensorChangedEventArgs e)
        {
            string before = e.StatusBefore?.ToString() ?? "немає";
            string after = e.StatusAfter?.ToString() ?? "немає";
            string line = $"{e.At:HH:mm:ss} {e.Kind,-12} {e.Id} " +
                          $"{before} -> {after} | {e.Location ?? "без локації"}";
            Console.WriteLine($"[EVENT] {line}");
            File.AppendAllText(logPath, line + Environment.NewLine);
            loggedLines++;
        }

        service.Changed += OnSensorChanged;

        EventHandler<SensorChangedEventArgs> counterHandler = (_, _) => changes++;
        service.Changed += counterHandler;

        Console.WriteLine("=== LAB06: event demo ===");
        Console.WriteLine($"Сховище: {(useFile ? "FileSensorStore" : "InMemorySensorStore")}");
        Console.WriteLine($"Початкових сенсорів: {service.All().Count}");

        var created = service.Add("ESP-LAB06", "DHT22", 1500);
        service.Mount(created.Id, "IoT-лабораторія №6");
        service.SendToMaintenance(created.Id);
        Console.WriteLine($"LAB06: events before unsubscribe = {changes}");

        // Failed domain invariant: no event and no log line.
        int beforeFailure = changes;
        try
        {
            service.Mount(created.Id, "");
            Console.WriteLine("[FAIL] Очікувана помилка не виникла.");
            Environment.ExitCode = 1;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"[OK] Сценарій відмови: {ex.Message}");
        }
        Console.WriteLine($"LAB06: failure did not raise event = {changes == beforeFailure}");

        // Unsubscribe only the logger. Counter stays subscribed.
        service.Changed -= OnSensorChanged;
        Console.WriteLine("=== LAB06: after -= ===");
        service.Mount(created.Id, "IoT-лабораторія №6");
        Console.WriteLine($"LAB06: events after unsubscribe = {changes}");
        Console.WriteLine($"LAB06: logged lines after unsubscribe = {loggedLines}");
        Console.WriteLine($"Статус сенсора після тихої операції: {service.Find(created.Id)?.Status}");

        var active = service.FindAll(s => s.Status == SensorStatus.Active);
        Console.WriteLine($"LAB06: FindAll active = {active.Count}");

        // Optional task 2: import callback with Action<string>.
        Console.WriteLine("=== LAB06: Action<string> import ===");
        int skipped = 0;
        var imported = SensorImportReporter.ImportLines(
            [
                "S-701;ESP-IMPORT;DHT22;1500",
                "некоректний рядок",
                "S-702;ESP-IMPORT;;1500",
                "S-703;ESP-IMPORT;HC-SR04;2500"
            ],
            message =>
            {
                skipped++;
                Console.WriteLine($"[IMPORT/SKIP] {message}");
            });
        Console.WriteLine($"LAB06: imported = {imported.Count}, skipped = {skipped}");
        Console.WriteLine($"LAB06: sinks = {(useSinks ? "enabled" : "disabled")}");
        Console.WriteLine($"Лог подій: {logPath}");
        if (useSinks) Console.WriteLine($"Лог додаткового FileSink: {sinkLogPath}");
        if (useFile) Console.WriteLine($"JSON-файл: {dataPath}");

        service.Changed -= counterHandler;
    }

    private static string FindRepositoryRoot()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "CrossApp.slnx")))
                    return dir.FullName;
                dir = dir.Parent;
            }
        }
        throw new DirectoryNotFoundException("Не знайдено CrossApp.slnx.");
    }
}
