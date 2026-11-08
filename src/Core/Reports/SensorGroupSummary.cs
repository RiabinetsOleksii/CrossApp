namespace Core.Reports;

public sealed record SensorGroupSummary(
    string Key,
    int Count,
    int TotalPingIntervalMs);
