using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IReadOnlyList<Sensor> Sensors()
    {
        var sensors = new List<Sensor>();
        for (var i = 1; i <= 18; i++)
        {
            var controllerId = $"ESP-{((i - 1) / 3 + 1):00}";
            // LAB07-SAMPLE-DATA: 4 model categories for GroupBy.
            var model = ((i - 1) % 4) switch
            {
                0 => "DHT22",
                1 => "HC-SR04",
                2 => "BMP280",
                _ => "PIR"
            };
            // LAB07-SAMPLE-DATA: 3 examples with interval < 1000ms.
            var intervalMs = i <= 3 ? 600 + i * 100 : 1000 + i * 100;
            var sensor = Sensor.Create($"S-{i:000}", controllerId, model, intervalMs);
            if (i % 3 == 0) sensor.MountAndActivate($"Зона {i}");
            sensors.Add(sensor);
        }
        return sensors;
    }
}
