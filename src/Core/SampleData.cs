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
            var model = i % 2 == 0 ? "DHT22" : "HC-SR04";
            var sensor = Sensor.Create($"S-{i:000}", controllerId, model, 1000 + i * 100);
            if (i % 3 == 0) sensor.MountAndActivate($"Зона {i}");
            sensors.Add(sensor);
        }
        return sensors;
    }
}
