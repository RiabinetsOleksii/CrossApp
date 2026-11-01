using Core.Domain;

namespace Core.Import;

// Optional task 2: a callback reports skipped rows; the caller decides how to display them.
// Format: Id;ControllerId;Model;PingIntervalMs
public static class SensorImportReporter
{
    public static IReadOnlyList<Sensor> ImportLines(
        IEnumerable<string> lines,
        Action<string> reportSkipped)
    {
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(reportSkipped);

        var imported = new List<Sensor>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var number = 0;

        foreach (var line in lines)
        {
            number++;
            var parts = (line ?? string.Empty).Split(';', StringSplitOptions.TrimEntries);
            if (parts.Length != 4)
            {
                reportSkipped($"Рядок {number}: очікується 4 поля, отримано {parts.Length}.");
                continue;
            }

            if (!int.TryParse(parts[3], out var interval))
            {
                reportSkipped($"Рядок {number}: некоректний інтервал '{parts[3]}'.");
                continue;
            }

            try
            {
                var sensor = Sensor.Create(parts[0], parts[1], parts[2], interval);
                if (!ids.Add(sensor.Id))
                {
                    reportSkipped($"Рядок {number}: дублікат ID '{sensor.Id}'.");
                    continue;
                }
                imported.Add(sensor);
            }
            catch (ArgumentException ex)
            {
                reportSkipped($"Рядок {number}: {ex.Message}");
            }
        }
        return imported;
    }
}
