using Core.Domain;

namespace Core.Events;

public sealed class SensorChangedEventArgs(
    string id,
    SensorChangeKind kind,
    SensorStatus? statusBefore,
    SensorStatus? statusAfter,
    string? location) : EventArgs
{
    public string Id { get; } = id;
    public SensorChangeKind Kind { get; } = kind;
    public SensorStatus? StatusBefore { get; } = statusBefore;
    public SensorStatus? StatusAfter { get; } = statusAfter;
    public string? Location { get; } = location;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}
