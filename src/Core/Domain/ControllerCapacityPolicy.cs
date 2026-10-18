namespace Core.Domain;

public static class ControllerCapacityPolicy
{
    public const int MaxActiveSensorsPerController = 3;

    public static void EnsureCanMountSensor(string controllerId, IReadOnlyList<Sensor> existingSensors)
    {
        if (string.IsNullOrWhiteSpace(controllerId))
            throw new ArgumentException("Ідентифікатор контролера обов'язковий", nameof(controllerId));

        int activeSensors = existingSensors.Count(s =>
            s.ControllerId == controllerId.Trim() &&
            s.Status == SensorStatus.Active);

        if (activeSensors >= MaxActiveSensorsPerController)
        {
            throw new InvalidOperationException(
                $"Контролер {controllerId} вже має {activeSensors} активних сенсорів (ліміт {MaxActiveSensorsPerController})");
        }
    }
}
