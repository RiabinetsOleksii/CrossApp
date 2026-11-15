using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

/// <summary>
/// Imports the same JSON array of SensorStorageDto that FileSensorStore writes.
/// True asynchronous I/O (not Task.Run); invalid domain records carry their row number.
/// FileNotFoundException, IOException, JsonException and cancellation are NOT masked.
/// </summary>
public sealed class JsonSensorImporter : ISensorImporter, IStreamingSensorImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<Sensor>> LoadAsync(
        string path, CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ct.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);
        var dtos = await JsonSerializer.DeserializeAsync<List<SensorStorageDto>>(
            stream, Options, cancellationToken: ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку сенсорів.");

        var result = new List<Sensor>(dtos.Count);
        if (dtos.Count == 0) progress?.Report(100);
        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(ConvertRow(dtos[i], i + 1));
            progress?.Report((int)((i + 1L) * 100 / dtos.Count));
        }
        return result;
    }

    // Optional #1: deserialize incrementally, using a cancellation-aware async iterator.
    public async IAsyncEnumerable<Sensor> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ct.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);
        int index = 0;
        await foreach (var dto in JsonSerializer.DeserializeAsyncEnumerable<SensorStorageDto>(
            stream, Options, cancellationToken: ct).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            yield return ConvertRow(dto, ++index);
        }
    }

    private static Sensor ConvertRow(SensorStorageDto? dto, int row)
    {
        if (dto is null)
            throw new DomainException($"Запис {row}: сенсор не може бути null.");
        try
        {
            return Sensor.FromStorageDto(dto);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException
                                   or InvalidDataException)
        {
            throw new DomainException($"Запис {row}: {ex.Message}", ex);
        }
    }
}
