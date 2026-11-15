using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Domain;

namespace Core.Tests;

internal static class TestData
{
    public static Sensor[] Items() =>
    [
        Sensor.Create("S-2", "ESP-1", "DHT22", 500),
        Sensor.Create("S-3", "ESP-1", "PIR", 900),
        Sensor.Create("S-1", "ESP-2", "BMP280", 900),
        Sensor.Create("S-4", "ESP-2", "DHT22", 1800),
    ];

    public static async Task<string> TempJsonAsync(params Sensor[] sensors)
    {
        var path = Path.Combine(Path.GetTempPath(),
            "crossapp_lab08_" + Guid.NewGuid().ToString("N") + ".json");
        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(
            sensors.Select(x => x.ToStorageDto()), options));
        return path;
    }

    public static async Task<string> RawJsonAsync(string json)
    {
        var path = Path.Combine(Path.GetTempPath(),
            "crossapp_lab08_" + Guid.NewGuid().ToString("N") + ".json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}
