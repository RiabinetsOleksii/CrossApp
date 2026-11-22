using System.Text.Json;
using Core;
using Core.Configuration;
using Core.Import;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public sealed class Lab09Tests
{
    [Fact]
    public void Options_ResolveRelativePathAgainstExplicitBase()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "crossapp-lab09-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = new StorageOptions { DataPath = "data", FileName = "sensors.json" };
            Assert.Equal(Path.GetFullPath(Path.Combine(baseDir, "data", "sensors.json")),
                options.ResolveFullPath(baseDir));
        }
        finally { if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true); }
    }

    [Fact]
    public void Options_AbsoluteDataPathOverridesBase_WithoutManualSlashes()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "crossapp-lab09-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = new StorageOptions { DataPath = baseDir, FileName = "export.json" };
            Assert.Equal(Path.GetFullPath(Path.Combine(baseDir, "export.json")),
                options.ResolveFullPath(Path.GetTempPath()));
        }
        finally { if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true); }
    }

    [Theory]
    [InlineData("../outside.json")]
    [InlineData("nested/file.json")]
    [InlineData("..")] 
    public void Options_RejectsUnsafeFileName(string name)
    {
        var options = new StorageOptions { FileName = name };
        Assert.Throws<ArgumentException>(() => options.ResolveFullPath(Path.GetTempPath()));
    }

    [Fact]
    public async Task FileStore_ExportsAtomicCamelCaseJson_WithReadableUkrainian_AndReimports()
    {
        var folder = Path.Combine(Path.GetTempPath(), "crossapp-lab09-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "sensors.json");
        try
        {
            var store = new FileSensorStore(path);
            var sensor = SampleData.Sensors()[2]; // Has location "Зона 3"
            store.Add(sensor);
            Assert.Equal(1, store.ExportSnapshot());
            var json = await File.ReadAllTextAsync(path);
            Assert.Contains("\"id\"", json);
            Assert.Contains("Зона 3", json);
            Assert.DoesNotContain("\\u0417", json);
            Assert.False(File.Exists(path + ".tmp"));
            Assert.Single(new FileSensorStore(path).List());
            Assert.Single(await new JsonSensorImporter().LoadAsync(path));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void FileStore_ReadsLegacyPascalCaseJson()
    {
        var folder = Path.Combine(Path.GetTempPath(), "crossapp-lab09-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "old.json");
        try
        {
            var sensor = SampleData.Sensors()[0];
            var dto = sensor.ToStorageDto();
            var options = new JsonSerializerOptions();
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            File.WriteAllText(path, JsonSerializer.Serialize(new[] { dto }, options));
            Assert.Single(new FileSensorStore(path).List());
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void FileStore_BadJsonThrowsJsonException()
    {
        var folder = Path.Combine(Path.GetTempPath(), "crossapp-lab09-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "broken.json");
        try
        {
            File.WriteAllText(path, "{invalid json");
            Assert.Throws<JsonException>(() => new FileSensorStore(path).List());
        }
        finally { Directory.Delete(folder, true); }
    }
    [Fact]
    public void FileStore_ExportSnapshot_ExportsAllExistingSensors()
    {
        var folder = Path.Combine(Path.GetTempPath(), "crossapp-lab09-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "complete.json");
        try
        {
            var store = new FileSensorStore(path);
            foreach (var sensor in SampleData.Sensors().Take(5))
                store.Add(sensor);
            Assert.Equal(5, store.ExportSnapshot());
            var json = File.ReadAllText(path);
            var dtos = JsonSerializer.Deserialize<List<Core.Dto.SensorStorageDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
            Assert.NotNull(dtos);
            Assert.Equal(5, dtos.Count);
            Assert.Equal(5, new FileSensorStore(path).List().Count);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public void Options_CreatesMissingFolder_ForPortableDataPath()
    {
        var folder = Path.Combine(Path.GetTempPath(), "crossapp-lab09-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = new StorageOptions { DataPath = "nested/data", FileName = "sensors.json" };
            var path = options.ResolveFullPath(folder);
            Assert.True(Directory.Exists(Path.GetDirectoryName(path)));
            Assert.Equal(Path.GetFullPath(Path.Combine(folder, "nested", "data", "sensors.json")), path);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

}
