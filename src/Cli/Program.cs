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

ImportResult<DeviceDomainDto> result = DeviceCsvImporter.Load(path);
Console.WriteLine($"Завантажено записів: {result.Items.Count}");

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