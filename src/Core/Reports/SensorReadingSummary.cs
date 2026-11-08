namespace Core.Reports;

public sealed record SensorReadingSummary(
    string SensorId,
    string Model,
    int ReadingCount,
    int TotalResponseTimeMs);
