namespace Core.Dto;

public abstract record DeviceDomainDto;

public record ControllerDto(string Id, string IpAddress, string Protocol) : DeviceDomainDto;

public record SensorDto(string Id, string Model, string? Location = null) : DeviceDomainDto;

public record TelemetryDto(string Id, string SensorId, double Value, string Timestamp) : DeviceDomainDto;