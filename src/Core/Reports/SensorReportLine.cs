namespace Core.Reports;

// Immutable, presentation-independent data contract for a row in reports.
public sealed record SensorReportLine(
    string Id,
    string Model,
    string ControllerId,
    int PingIntervalMs,
    string Status);
