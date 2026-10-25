using Core.Domain;

namespace Core.Dto;

// Full storage snapshot; the old SensorDto remains unchanged for labs 1-4.
public sealed record SensorStorageDto(
    string Id,
    string ControllerId,
    string Model,
    int PingIntervalMs,
    string? Location,
    SensorStatus Status);
