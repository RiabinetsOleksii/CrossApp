using Core.Domain;

namespace Core.Reports;

// LINQ to Objects only. Core knows neither Console nor a particular data store.
public static class SensorReportService
{
    private static SensorReportLine Line(Sensor sensor) =>
        new(sensor.Id, sensor.Model, sensor.ControllerId,
            sensor.PingIntervalMs, sensor.Status.ToString());

    // 1: filtering + two-level sorting + projection. Small interval means high polling frequency.
    public static IReadOnlyList<SensorReportLine> LowInterval(
        IEnumerable<Sensor> source, int thresholdMs)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (thresholdMs <= 0) throw new ArgumentOutOfRangeException(nameof(thresholdMs));
        return source.Where(sensor => sensor.PingIntervalMs < thresholdMs)
            .OrderBy(sensor => sensor.PingIntervalMs)
            .ThenBy(sensor => sensor.Id, StringComparer.Ordinal)
            .Select(Line)
            .ToList();
    }

    // 2: stable top-N, using generic extension (optional task #2, usage 1).
    public static IReadOnlyList<SensorReportLine> TopByInterval(
        IEnumerable<Sensor> source, int n)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.TopBy(sensor => sensor.PingIntervalMs, n, sensor => sensor.Id)
            .Select(Line)
            .ToList();
    }

    // 3: GroupBy + Count + Sum + sorted projection into a record DTO.
    public static IReadOnlyList<SensorGroupSummary> ByModel(IEnumerable<Sensor> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.GroupBy(sensor => sensor.Model, StringComparer.OrdinalIgnoreCase)
            .Select(group => new SensorGroupSummary(
                group.Key, group.Count(), group.Sum(s => s.PingIntervalMs)))
            .OrderByDescending(summary => summary.TotalPingIntervalMs)
            .ThenBy(summary => summary.Key, StringComparer.Ordinal)
            .ToList();
    }

    // 4: empty input is legal (Average and Max would otherwise throw).
    public static (int Items, int Total, double Average, int Max)
        Totals(IEnumerable<Sensor> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count,
                list.Sum(s => s.PingIntervalMs),
                list.Average(s => s.PingIntervalMs),
                list.Max(s => s.PingIntervalMs));
    }

    // Optional #1: left GroupJoin sensors x demonstration readings. Sensors without readings remain.
    public static IReadOnlyList<SensorReadingSummary> ReadingsBySensor(
        IEnumerable<Sensor> sensors, IEnumerable<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(sensors);
        ArgumentNullException.ThrowIfNull(readings);
        return sensors.GroupJoin(readings,
                sensor => sensor.Id,
                reading => reading.SensorId,
                (sensor, group) => new SensorReadingSummary(
                    sensor.Id, sensor.Model, group.Count(), group.Sum(x => x.ResponseTimeMs)),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(line => line.SensorId, StringComparer.Ordinal)
            .ToList();
    }

    // Optional #2: generic TopBy<T> used again, this time on reading-summary DTOs.
    public static IReadOnlyList<SensorReadingSummary> TopByReadings(
        IEnumerable<SensorReadingSummary> summaries, int n)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        return summaries.TopBy(x => x.ReadingCount, n, x => x.SensorId);
    }

    // Optional #3: equivalent to ByModel, expressed with LINQ query syntax.
    public static IReadOnlyList<SensorGroupSummary> ByModelQuerySyntax(
        IEnumerable<Sensor> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var query = from sensor in source
                    group sensor by sensor.Model.ToUpperInvariant() into groupOfSensors
                    orderby groupOfSensors.Sum(s => s.PingIntervalMs) descending,
                        groupOfSensors.Key ascending
                    select new SensorGroupSummary(
                        groupOfSensors.Key,
                        groupOfSensors.Count(),
                        groupOfSensors.Sum(s => s.PingIntervalMs));
        return query.ToList();
    }
}
