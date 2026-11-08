namespace Core.Reports;

public static class EnumerableReportExtensions
{
    // Optional task #2: generic TopBy<T>. The explicit unique key makes ties deterministic.
    // Applied to two different report DTO / source types in SensorReportService.
    public static IReadOnlyList<T> TopBy<T>(
        this IEnumerable<T> source,
        Func<T, int> scoreSelector,
        int n,
        Func<T, string> uniqueKeySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(scoreSelector);
        ArgumentNullException.ThrowIfNull(uniqueKeySelector);
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), "N must be >= 0.");

        return source.OrderByDescending(scoreSelector)
            .ThenBy(uniqueKeySelector, StringComparer.Ordinal)
            .Take(n)
            .ToList();
    }
}
