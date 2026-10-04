using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class DeviceCsvImporter
{
    private const char Separator = ';';

    public static ImportResult<DeviceDomainDto> Load(string path)
    {
        var items = new List<DeviceDomainDto>();
        var errors = new List<string>();
        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line)) continue;

            if (number == 1 && line.StartsWith("Type", StringComparison.OrdinalIgnoreCase)) continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }
        return new ImportResult<DeviceDomainDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 3 } => new ParseFailed($"очікується мінімум 3 колонки, отримано {parts.Length}"),
            ["C", var id, var ip, var protocol] when string.IsNullOrWhiteSpace(ip)
                => new ParseFailed("IP-адреса порожня"),
            ["C", var id, var ip, var protocol]
                => new ParseOk(new ControllerDto(id, ip, protocol)),
            ["S", var id, var model, var location]
                => new ParseOk(new SensorDto(id, model, string.IsNullOrWhiteSpace(location) ? null : location)),
            ["S", var id, var model]
                => new ParseOk(new SensorDto(id, model)),
            ["T", var id, var sensorId, var valStr, var timestamp] when !double.TryParse(valStr, NumberStyles.Number, CultureInfo.InvariantCulture, out double val)
                => new ParseFailed($"Значення '{valStr}' не є коректним числом"),
            ["T", var id, var sensorId, var valStr, var timestamp]
                => new ParseOk(new TelemetryDto(id, sensorId, double.Parse(valStr, CultureInfo.InvariantCulture), timestamp)),
            [var type, ..] => new ParseFailed($"Невідомий префікс: '{type}'"),
            _ => new ParseFailed("Некоректний формат рядка")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(DeviceDomainDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}