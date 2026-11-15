using System.Runtime.CompilerServices;
using System.Text.Json;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Import;
using Core.Services;
using Core.Storage;

internal static class Lab08Demo
{
    public static async Task RunAsync(string[] args)
    {
        var command = args.FirstOrDefault(x => x.StartsWith("import-", StringComparison.OrdinalIgnoreCase))
            ?? "import-async";
        var input = Option(args, "--path") ?? Path.Combine("data", "lab08-valid.json");
        if (command.Equals("import-invalid", StringComparison.OrdinalIgnoreCase))
            input = Option(args, "--path") ?? Path.Combine("data", "lab08-invalid.json");
        input = Path.GetFullPath(input);
        var timeoutMs = IntOption(args, "--timeout-ms", 0);
        var delayMs = IntOption(args, "--delay-ms", 0);
        bool file = args.Contains("--file", StringComparer.OrdinalIgnoreCase);
        ISensorStore store = file
            ? new FileSensorStore(ProjectPaths.GetSensorDataPath(args))
            : new InMemorySensorStore();
        var service = new SensorService(store);
        var importer = new JsonSensorImporter();
        int eventCount = 0;
        service.Changed += (_, e) =>
        {
            eventCount++;
            Console.WriteLine($"LAB08: event #{eventCount}: {e.Kind} {e.Id}");
        };

        using var user = new CancellationTokenSource();
        using var timeout = new CancellationTokenSource();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(user.Token, timeout.Token);
        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; user.Cancel(); };
        Console.CancelKeyPress += onCancel;
        try
        {
            if (command == "import-cancel") user.Cancel();
            if (command == "import-timeout")
            {
                timeoutMs = timeoutMs > 0 ? timeoutMs : 50;
                delayMs = delayMs > 0 ? delayMs : 2000;
            }
            if (timeoutMs > 0) timeout.CancelAfter(timeoutMs);
            Console.WriteLine($"LAB08: mode={command}; input={input}");
            Console.WriteLine("Ctrl+C — скасувати імпорт; підтримується --timeout-ms і --delay-ms.");

            int added;
            if (command == "import-stream")
            {
                IStreamingSensorImporter streaming = delayMs > 0
                    ? new DelayedStreamImporter(importer, delayMs) : importer;
                added = await service.ImportStreamAsync(streaming, input, linked.Token);
            }
            else if (command is "import-async" or "import-progress" or "import-invalid"
                     or "import-cancel" or "import-timeout")
            {
                ISensorImporter selected = delayMs > 0
                    ? new DelayedImporter(importer, delayMs) : importer;
                IProgress<int>? progress = command == "import-progress"
                    ? new ConsoleProgress() : null;
                added = await service.ImportAsync(selected, input, linked.Token, progress);
            }
            else
            {
                Console.WriteLine("Команди: import-async | import-stream | import-progress | " +
                    "import-cancel | import-timeout | import-invalid");
                Environment.ExitCode = 1;
                return;
            }
            Console.WriteLine($"LAB08: added={added}, events={eventCount}, stored={service.All().Count}");
        }
        catch (OperationCanceledException)
        {
            if (timeout.IsCancellationRequested && !user.IsCancellationRequested)
                Console.WriteLine("Імпорт скасовано: спрацював тайм-аут.");
            else
                Console.WriteLine("Імпорт скасовано.");
            Console.WriteLine($"LAB08: timeout-fired={timeout.IsCancellationRequested}, user-cancelled={user.IsCancellationRequested}");
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"Дані відхилено: {ex.Message}");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Файл не знайдено: {input}");
            Environment.ExitCode = 1;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Технічна помилка JSON: {ex.Message}");
            Environment.ExitCode = 1;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Помилка читання файлу: {ex.Message}");
            Environment.ExitCode = 1;
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    private static string? Option(string[] args, string key)
    {
        var index = Array.FindIndex(args, x => x.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return null;
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--"))
            throw new ArgumentException($"Після {key} потрібно вказати значення.");
        return args[index + 1];
    }

    private static int IntOption(string[] args, string name, int fallback)
    {
        var value = Option(args, name);
        if (value is null) return fallback;
        if (!int.TryParse(value, out int result) || result < 0)
            throw new ArgumentOutOfRangeException(name, "Потрібне ціле невід'ємне число.");
        return result;
    }

    private sealed class ConsoleProgress : IProgress<int>
    {
        private int _last = -1;
        public void Report(int value)
        {
            if (value != _last)
            {
                Console.WriteLine($"LAB08: progress={value}%");
                _last = value;
            }
        }
    }

    // Only demonstration delay; actual file reading in JsonSensorImporter remains async I/O.
    private sealed class DelayedImporter(ISensorImporter inner, int delayMs) : ISensorImporter
    {
        public async Task<IReadOnlyList<Sensor>> LoadAsync(
            string path, CancellationToken ct = default, IProgress<int>? progress = null)
        {
            await Task.Delay(delayMs, ct);
            return await inner.LoadAsync(path, ct, progress);
        }
    }

    private sealed class DelayedStreamImporter(IStreamingSensorImporter inner, int delayMs)
        : IStreamingSensorImporter
    {
        public async IAsyncEnumerable<Sensor> StreamAsync(
            string path, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var item in inner.StreamAsync(path, ct).WithCancellation(ct))
            {
                await Task.Delay(delayMs, ct);
                yield return item;
            }
        }
    }
}
