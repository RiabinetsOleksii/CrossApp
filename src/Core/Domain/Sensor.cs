using Core.Dto;

namespace Core.Domain;

public sealed class Sensor
{
    private string? _location;
    private int _pingIntervalMs;

    public string Id { get; }
    public string ControllerId { get; }
    public string Model { get; }
    public string? Location => _location;
    public int PingIntervalMs => _pingIntervalMs;
    public SensorStatus Status { get; private set; } = SensorStatus.New;

    private Sensor(string id, string controllerId, string model, int pingIntervalMs, string? location)
    {
        Id = id;
        ControllerId = controllerId;
        Model = model;
        _pingIntervalMs = pingIntervalMs;
        _location = location;
    }

    public static Sensor Create(string id, string controllerId, string model, int pingIntervalMs, string? location = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(controllerId))
            throw new ArgumentException("Прив'язка до контролера обов'язкова", nameof(controllerId));
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Модель не може бути порожньою", nameof(model));
        if (pingIntervalMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(pingIntervalMs), pingIntervalMs, "Інтервал опитування має бути > 0");

        return new Sensor(id.Trim(), controllerId.Trim(), model.Trim().ToUpperInvariant(), pingIntervalMs, location?.Trim());
    }

    private void TransitionTo(SensorStatus next)
    {
        bool allowed = (Status, next) switch
        {
            (SensorStatus.New, SensorStatus.Active) => true,
            (SensorStatus.Active, SensorStatus.Maintenance) => true,
            (SensorStatus.Maintenance, SensorStatus.Active) => true,
            (SensorStatus.Active, SensorStatus.Broken) => true,
            (SensorStatus.Maintenance, SensorStatus.Broken) => true,
            _ => false
        };
        if (!allowed)
            throw new InvalidOperationException($"Сенсор {Id}: перехід {Status} -> {next} неможливий");
        Status = next;
    }

    public void MountAndActivate(string newLocation)
    {
        if (string.IsNullOrWhiteSpace(newLocation))
            throw new ArgumentException("Нова локація не може бути порожньою", nameof(newLocation));
        TransitionTo(SensorStatus.Active);
        _location = newLocation.Trim();
    }

    public void SendToMaintenance() => TransitionTo(SensorStatus.Maintenance);
    public void MarkAsBroken() => TransitionTo(SensorStatus.Broken);

    // Kept intact for existing CSV/JSON import scenarios (labs 1-4).
    public SensorDto ToDto() => new(Id, Model, Location);

    public static Sensor FromDto(SensorDto dto, string controllerId = "UNKNOWN")
    {
        var sensor = Create(dto.Id, controllerId, dto.Model, 1000, dto.Location);
        if (dto.Location != null) sensor.TransitionTo(SensorStatus.Active);
        return sensor;
    }

    // NEW IN LAB 5: complete state saved to disk rather than lossy SensorDto.
    public SensorStorageDto ToStorageDto() =>
        new(Id, ControllerId, Model, PingIntervalMs, Location, Status);

    public static Sensor FromStorageDto(SensorStorageDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // Rehydrate via public domain operations: do NOT expose setters or bypass invariants.
        var sensor = Create(dto.Id, dto.ControllerId, dto.Model, dto.PingIntervalMs, dto.Location);
        switch (dto.Status)
        {
            case SensorStatus.New:
                break;
            case SensorStatus.Active:
                sensor.MountAndActivate(RequiredLocation(dto));
                break;
            case SensorStatus.Maintenance:
                sensor.MountAndActivate(RequiredLocation(dto));
                sensor.SendToMaintenance();
                break;
            case SensorStatus.Broken:
                sensor.MountAndActivate(RequiredLocation(dto));
                sensor.MarkAsBroken();
                break;
            default:
                throw new InvalidDataException($"Невідомий статус сенсора {dto.Id}: {dto.Status}");
        }
        return sensor;
    }

    private static string RequiredLocation(SensorStorageDto dto) =>
        !string.IsNullOrWhiteSpace(dto.Location)
            ? dto.Location
            : throw new InvalidDataException($"Сенсор {dto.Id} зі статусом {dto.Status} повинен мати локацію.");

    public override string ToString() =>
        $"Сенсор {Id} [{Status}] (Контролер: {ControllerId}) | {Model} | Локація: {Location ?? "Немає"}";
}
