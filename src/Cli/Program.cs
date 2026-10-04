using System.Text;
using Core.Dto;
using Core.Import;

Console.OutputEncoding = Encoding.UTF8;
string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

// Вибір імпортера за розширенням файлу
ImportResult<DeviceDomainDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => DeviceCsvImporter.Load(path),
    ".json" => DeviceJsonImporter.Load(path),
    var ext => new ImportResult<DeviceDomainDto>([], [$"Непідтримуваний формат файлу: {ext}"])
};

// Статистика імпорту одним рядком
int accepted = result.Items.Count;
int skipped = result.Errors.Count;
int total = accepted + skipped;
double errorRate = total > 0 ? (double)skipped / total * 100 : 0;

Console.WriteLine($"Статистика: усього {total} | прийнято {accepted} | пропущено {skipped} | помилок {errorRate:F1}%\n");

foreach (DeviceDomainDto item in result.Items)
{
    string output = item switch
    {
        ControllerDto c => $"[Контролер] {c.Id,-6} IP: {c.IpAddress,-15} Протокол: {c.Protocol}",
        SensorDto s => $"[Сенсор]    {s.Id,-6} Модель: {s.Model,-11} Локація: {s.Location ?? "Не вказано"}",
        TelemetryDto t => $"[Дані]      {t.Id,-6} Сенсор: {t.SensorId,-7} Значення: {t.Value,-5} Час: {t.Timestamp}",
        _ => item.ToString()
    };
    Console.WriteLine(output);
}

if (result.Errors.Count > 0)
{
    Console.WriteLine($"\nПропущено рядків: {result.Errors.Count}");
    foreach (string error in result.Errors)
    {
        Console.WriteLine($" ! {error}");
    }
}
return 0;