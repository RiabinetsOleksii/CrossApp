using Xunit;
using System.Text.Json;
using Core;
using Core.Domain;
using Core.Import;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public sealed class ImportTests
{
    [Fact]
    public async Task JsonImporter_ReadsRealJsonAsynchronously_AndReportsProgress()
    {
        var path = await TestData.TempJsonAsync(TestData.Items());
        try
        {
            var progress = new RecordedProgress();
            var items = await new JsonSensorImporter().LoadAsync(path, progress: progress);
            Assert.Equal(4, items.Count);
            Assert.Equal(100, progress.Values.Last());
            Assert.Equal("S-2", items[0].Id);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task InvalidRecord_ThrowsDomainException_WithRowNumberAndInner()
    {
        var path = await TestData.RawJsonAsync("""
            [{"Id":"S-bad","ControllerId":"ESP","Model":"PIR","PingIntervalMs":0,"Location":null,"Status":"New"}]
            """);
        try
        {
            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                new JsonSensorImporter().LoadAsync(path));
            Assert.Contains("Запис 1", ex.Message);
            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task MalformedJson_IsTechnicalJsonException_NotDomainException()
    {
        var path = await TestData.RawJsonAsync("{invalid json");
        try
        {
            await Assert.ThrowsAsync<JsonException>(() =>
                new JsonSensorImporter().LoadAsync(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task MissingFile_IsTechnicalFileNotFoundException()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            new JsonSensorImporter().LoadAsync(path));
    }

    [Fact]
    public async Task StreamingImport_UsesAwaitForeach_RetainsIdempotencyAndEvents()
    {
        var path = await TestData.TempJsonAsync(TestData.Items());
        try
        {
            var service = new SensorService(new InMemorySensorStore());
            int events = 0;
            service.Changed += (_, _) => events++;
            Assert.Equal(4, await service.ImportStreamAsync(new JsonSensorImporter(), path));
            Assert.Equal(0, await service.ImportStreamAsync(new JsonSensorImporter(), path));
            Assert.Equal(4, events);
            Assert.Equal(4, service.All().Count);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task StreamingImport_RespectsAlreadyCancelledToken()
    {
        var path = await TestData.TempJsonAsync(TestData.Items());
        try
        {
            var service = new SensorService(new InMemorySensorStore());
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ImportStreamAsync(new JsonSensorImporter(), path, cts.Token));
            Assert.Empty(service.All());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task EmptyJsonArray_GivesNoItemsAndProgress100()
    {
        var path = await TestData.RawJsonAsync("[]");
        try
        {
            var progress = new RecordedProgress();
            Assert.Empty(await new JsonSensorImporter().LoadAsync(path, progress: progress));
            Assert.Contains(100, progress.Values);
        }
        finally { File.Delete(path); }
    }

    private sealed class RecordedProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];
        public void Report(int value) => Values.Add(value);
    }
}
