using System.Text;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = Encoding.UTF8;

// Previous work is preserved and available with --lab04.
if (args.Contains("--lab04", StringComparer.OrdinalIgnoreCase))
{
    Lab04Demo.Run();
    return;
}

try
{
    bool useFile = args.Contains("--file", StringComparer.OrdinalIgnoreCase);
    bool useCache = args.Contains("--cache", StringComparer.OrdinalIgnoreCase);
    bool useFactory = args.Contains("--factory", StringComparer.OrdinalIgnoreCase);
    string dataPath = ProjectPaths.GetSensorDataPath(args);

    // Composition root: concrete stores are chosen only in Program.cs.
    ISensorStore store = useFactory
        ? StoreFactory.Create(args)
        : useFile
            ? new FileSensorStore(dataPath)
            : new InMemorySensorStore(SampleData.Sensors());

    // Seed the file once; do not overwrite existing file contents on next run.
    if (useFile && store.List().Count == 0)
    {
        foreach (var sensor in SampleData.Sensors())
            store.Add(sensor);
    }

    if (useCache)
        store = new CachingSensorStore(store);

    var service = new SensorService(store);
    Console.WriteLine($"Сховище: {(useFile ? nameof(FileSensorStore) : nameof(InMemorySensorStore))}" +
                      (useCache ? " + CachingSensorStore" : ""));
    Console.WriteLine($"Спосіб вибору: {(useFactory ? "StoreFactory" : "тернарний оператор у Program.cs")}");
    Console.WriteLine($"Записів до сценарію: {service.All().Count}");
    if (useFile) Console.WriteLine($"JSON-файл: {dataPath}");

    Console.WriteLine("\n=== Лабораторна №5: додати, змінити, знайти, показати ===");
    var created = service.Add("ESP-DEMO", "DHT22", 1500);
    service.Mount(created.Id, "IoT-лабораторія");
    Console.WriteLine($"Знайдено за ID: {service.Find(created.Id)}");
    Console.WriteLine($"Записів після сценарію: {service.All().Count}");
    foreach (var sensor in service.All().TakeLast(5))
        Console.WriteLine($" - {sensor}");

    Console.WriteLine("\n=== Додаткове №2: пошук через Func<Sensor, bool> ===");
    var active = service.Search(s => s.Status == SensorStatus.Active);
    Console.WriteLine($"Активних сенсорів: {active.Count}");

    Console.WriteLine("\n=== Сценарії контрольованих помилок ===");
    ShowExpectedError("Дубль ID", () => store.Add(created));
    ShowExpectedError("Невідомий ID", () => service.Mount("S-NOT-EXIST", "Невідома зона"));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Помилка запуску: {ex.Message}");
    Environment.ExitCode = 1;
}

static void ShowExpectedError(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"[FAIL] {title}: помилку не виявлено.");
        Environment.ExitCode = 1;
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"[OK] {title}: {ex.Message}");
    }
}

// All concrete store creation remains in the CLI composition-root file.
internal static class StoreFactory
{
    public static ISensorStore Create(string[] args)
    {
        bool file = args.Contains("--file", StringComparer.OrdinalIgnoreCase);
        string path = ProjectPaths.GetSensorDataPath(args);
        return file
            ? new FileSensorStore(path)
            : new InMemorySensorStore(SampleData.Sensors());
    }
}

internal static class ProjectPaths
{
    public static string GetSensorDataPath(string[] args)
    {
        int option = Array.FindIndex(args, a =>
            string.Equals(a, "--data", StringComparison.OrdinalIgnoreCase));
        if (option >= 0)
        {
            if (option + 1 >= args.Length || args[option + 1].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException("Після --data потрібно вказати шлях до JSON-файлу.");
            return Path.GetFullPath(args[option + 1]);
        }

        return Path.Combine(FindRepositoryRoot(), "data", "sensors.json");
    }

    private static string FindRepositoryRoot()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            DirectoryInfo? dir = new DirectoryInfo(start);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "CrossApp.slnx")))
                    return dir.FullName;
                dir = dir.Parent;
            }
        }
        throw new DirectoryNotFoundException("Не знайдено CrossApp.slnx; запусти програму з папки CrossApp.");
    }
}
