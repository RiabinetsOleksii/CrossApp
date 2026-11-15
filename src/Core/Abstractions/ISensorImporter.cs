using Core.Domain;

namespace Core.Abstractions;

/// <summary>Abstraction allows unit tests to inject a fake importer without disk I/O.</summary>
public interface ISensorImporter
{
    Task<IReadOnlyList<Sensor>> LoadAsync(
        string path, CancellationToken ct = default, IProgress<int>? progress = null);
}
