using Core.Dto;

namespace Core.Domain;

public sealed class Sensor
{
    private string? _location;
    private bool _isActive;
    private int _pingIntervalMs;

    public string Id { get; }
    public string Model { get; }
    public string? Location => _location;
    public bool IsActive => _isActive;
    public int PingIntervalMs => _pingIntervalMs;

    private Sensor(string id, string model, int pingIntervalMs, string? location, bool isActive)
    {
        Id = id;
        Model = model;
        _pingIntervalMs = pingIntervalMs;
        _location = location;
        _isActive = isActive;
    }

    public static Sensor Create(string id, string model, int pingIntervalMs, string? location = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Модель не може бути порожньою", nameof(model));

        if (pingIntervalMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(pingIntervalMs), pingIntervalMs,
                "Інтервал опитування має бути більшим за нуль");

        return new Sensor(id.Trim(), model.Trim().ToUpperInvariant(), pingIntervalMs, location?.Trim(), true);
    }

    public void Mount(string newLocation)
    {
        if (string.IsNullOrWhiteSpace(newLocation))
            throw new ArgumentException("Нова локація не може бути порожньою", nameof(newLocation));

        if (!_isActive)
            throw new InvalidOperationException($"Не можна змонтувати деактивований сенсор {Id}");

        _location = newLocation.Trim();
    }

    public void Deactivate()
    {
        if (!_isActive)
            throw new InvalidOperationException($"Сенсор {Id} вже деактивовано");

        _isActive = false;
    }

    public SensorDto ToDto() => new(Id, Model, Location);

    public static Sensor FromDto(SensorDto dto) => Create(dto.Id, dto.Model, 1000, dto.Location);

    public override string ToString() => $"Сенсор {Id} [{Model}] | Інтервал: {PingIntervalMs}мс | Локація: {Location ?? "Немає"} | Активний: {IsActive}";
}
