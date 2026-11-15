using Xunit;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class SensorServiceTests
{
    [Fact]
    public void InvalidTransition_ThrowsDomainException_AndKeepsState()
    {
        var sensor = Sensor.Create("S-X", "ESP", "DHT22", 1000);
        Assert.Throws<DomainException>(() => sensor.SendToMaintenance());
        Assert.Equal(SensorStatus.New, sensor.Status);
    }

    [Fact]
    public void BadInput_ThrowsArgumentException_AndDoesNotCreate()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Sensor.Create("S-X", "ESP", "DHT22", 0));
    }

    [Fact]
    public void Add_RaisesChangedExactlyOnce()
    {
        var service = new SensorService(new InMemorySensorStore());
        var events = new List<SensorChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        var sensor = service.Add("ESP", "PIR", 1000);
        Assert.Single(events);
        Assert.Equal(SensorChangeKind.Added, events[0].Kind);
        Assert.Equal(sensor.Id, events[0].Id);
    }

    [Fact]
    public void Failure_DoesNotRaiseChanged()
    {
        var service = new SensorService(new InMemorySensorStore());
        int events = 0;
        service.Changed += (_, _) => events++;
        Assert.Throws<DomainException>(() => service.Mount("NOT-FOUND", "Zone"));
        Assert.Equal(0, events);
    }

    [Fact]
    public async Task ImportAsync_OnlyAddsNewRecords_AndRaisesEventsOnce()
    {
        var store = new InMemorySensorStore();
        var service = new SensorService(store);
        int events = 0;
        service.Changed += (_, _) => events++;
        var importer = new FakeImporter(TestData.Items());
        Assert.Equal(4, await service.ImportAsync(importer, "unused"));
        Assert.Equal(0, await service.ImportAsync(importer, "unused"));
        Assert.Equal(4, events);
        Assert.Equal(4, store.List().Count);
    }

    [Fact]
    public async Task ImportAsync_WithCancelledToken_DoesNotCallImporterOrChangeState()
    {
        var importer = new FakeImporter(TestData.Items());
        var store = new InMemorySensorStore();
        var service = new SensorService(store);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ImportAsync(importer, "unused", cts.Token));
        Assert.Equal(0, importer.Calls);
        Assert.Empty(store.List());
    }

    [Fact]
    public async Task LinkedTokens_TimeoutCancelsAndUserTokenIsNotCancelled()
    {
        using var user = new CancellationTokenSource();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(60));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(user.Token, timeout.Token);
        var service = new SensorService(new InMemorySensorStore());
        var importer = new HangingImporter();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ImportAsync(importer, "unused", linked.Token));
        Assert.True(timeout.IsCancellationRequested);
        Assert.False(user.IsCancellationRequested);
    }

    private sealed class FakeImporter(Sensor[] items) : ISensorImporter
    {
        public int Calls { get; private set; }
        public Task<IReadOnlyList<Sensor>> LoadAsync(
            string path, CancellationToken ct = default, IProgress<int>? progress = null)
        {
            Calls++;
            ct.ThrowIfCancellationRequested();
            progress?.Report(100);
            return Task.FromResult<IReadOnlyList<Sensor>>(items);
        }
    }

    private sealed class HangingImporter : ISensorImporter
    {
        public async Task<IReadOnlyList<Sensor>> LoadAsync(
            string path, CancellationToken ct = default, IProgress<int>? progress = null)
        {
            await Task.Delay(Timeout.Infinite, ct);
            return Array.Empty<Sensor>();
        }
    }
}
