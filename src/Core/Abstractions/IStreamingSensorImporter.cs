using Core.Domain;

namespace Core.Abstractions;

/// <summary>Optional Lab 8: stream records instead of buffering the complete list.</summary>
public interface IStreamingSensorImporter
{
    IAsyncEnumerable<Sensor> StreamAsync(string path, CancellationToken ct = default);
}
