using Xunit;
using Core.Domain;
using Core.Reports;

namespace Core.Tests;

public sealed class ReportServiceTests
{
    [Theory]
    [InlineData(1000, 3)]
    [InlineData(500, 0)]
    [InlineData(600, 1)]
    public void LowInterval_UsesStrictThreshold(int threshold, int expected)
    {
        var result = SensorReportService.LowInterval(TestData.Items(), threshold);
        Assert.Equal(expected, result.Count);
        Assert.All(result, s => Assert.True(s.PingIntervalMs < threshold));
    }

    [Fact]
    public void TopN_DeterministicEvenWhenOriginalOrderIsReversed()
    {
        var items = TestData.Items();
        var forward = SensorReportService.TopByInterval(items, 3);
        var backward = SensorReportService.TopByInterval(items.Reverse(), 3);
        Assert.True(forward.SequenceEqual(backward));
        Assert.Equal(new[] { "S-4", "S-1", "S-3" }, forward.Select(x => x.Id));
    }

    [Fact]
    public void EmptyCollections_AreHandledWithoutException()
    {
        Assert.Empty(SensorReportService.LowInterval(Array.Empty<Sensor>(), 1000));
        Assert.Empty(SensorReportService.ByModel(Array.Empty<Sensor>()));
        Assert.Empty(SensorReportService.TopByInterval(Array.Empty<Sensor>(), 5));
        Assert.Equal((0, 0, 0d, 0), SensorReportService.Totals(Array.Empty<Sensor>()));
    }

    [Fact]
    public void QuerySyntax_GivesTheSameResultsAsMethodSyntax()
    {
        var items = TestData.Items();
        Assert.True(SensorReportService.ByModel(items).SequenceEqual(
            SensorReportService.ByModelQuerySyntax(items)));
    }
}
