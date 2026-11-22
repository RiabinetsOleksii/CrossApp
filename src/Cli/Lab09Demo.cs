using System.Text.Json;
using Core;
using Core.Import;
using Core.Services;
using Core.Storage;

internal static class Lab09Demo
{
    public static async Task RunAsync(string[] args)
    {
        try
        {
            var settings = Lab09Configuration.Load(args);
            var command = args.FirstOrDefault(a =>
                a.Equals("config", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("export", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("import", StringComparison.OrdinalIgnoreCase)) ?? "config";

            Console.WriteLine($"LAB09: env={settings.EnvironmentName}; log={settings.LoggingLevel}");
            Console.WriteLine($"LAB09: path={settings.DataFile}");
            Console.WriteLine(settings.HasApiKey
                ? $"LAB09: ApiKey=present,length={settings.ApiKeyLength}"
                : "LAB09: ApiKey=absent");

            if (command.Equals("config", StringComparison.OrdinalIgnoreCase))
                return;

            if (command.Equals("export", StringComparison.OrdinalIgnoreCase))
            {
                var store = new FileSensorStore(settings.DataFile);
                // On the first run, create a small illustrative collection.
                // Existing user data are always preserved, never re-seeded.
                if (store.List().Count == 0)
                {
                    foreach (var item in SampleData.Sensors().Take(3))
                        store.Add(item);
                }
                // Explicitly export the entire stored collection (atomic rewrite).
                var count = store.ExportSnapshot();
                Console.WriteLine($"LAB09: exported={count}");
                return;
            }

            if (command.Equals("import", StringComparison.OrdinalIgnoreCase))
            {
                var service = new SensorService(new InMemorySensorStore());
                var added = await service.ImportAsync(new JsonSensorImporter(), settings.DataFile);
                Console.WriteLine($"LAB09: imported={added}; stored={service.All().Count}");
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"LAB09: file-not-found: {ex.FileName ?? ex.Message}");
            Environment.ExitCode = 1;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"LAB09: invalid-json: {ex.Message}");
            Environment.ExitCode = 1;
        }
        catch (Core.DomainException ex)
        {
            Console.Error.WriteLine($"LAB09: data-rejected: {ex.Message}");
            Environment.ExitCode = 1;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or IOException)
        {
            Console.Error.WriteLine($"LAB09: error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }
}
