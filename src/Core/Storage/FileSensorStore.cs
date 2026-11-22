using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileSensorStore(string path) : ISensorStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly Dictionary<string, Sensor> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;

        if (File.Exists(_path))
        {
            var json = File.ReadAllText(_path);
            var dtos = JsonSerializer.Deserialize<List<SensorStorageDto>>(json, Options) ?? [];
            foreach (var dto in dtos)
            {
                var sensor = Sensor.FromStorageDto(dto);
                if (!_cache.TryAdd(sensor.Id, sensor))
                    throw new InvalidDataException($"У файлі є дублікат id={sensor.Id}.");
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var dtos = _cache.Values.Select(s => s.ToStorageDto()).ToList();
        var temp = _path + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(dtos, Options));
            File.Move(temp, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    // Lab 9: export the full collection with the same atomic JSON writer.
    public int ExportSnapshot()
    {
        EnsureLoaded();
        Flush();
        return _cache.Count;
    }
    public IReadOnlyList<Sensor> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Sensor? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Sensor item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (!_cache.TryAdd(item.Id, item))
            throw new InvalidOperationException($"Сенсор з id={item.Id} уже існує.");
        Flush();
    }

    public void Update(Sensor item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        if (!_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Сенсор з id={item.Id} не знайдено для оновлення.");
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }
}
