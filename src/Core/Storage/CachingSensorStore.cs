using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// Optional task 1: decorator with the exact same ISensorStore interface.
public sealed class CachingSensorStore(ISensorStore inner) : ISensorStore
{
    private readonly ISensorStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private IReadOnlyList<Sensor>? _listCache;
    private readonly Dictionary<string, Sensor?> _byIdCache =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Sensor> List() => _listCache ??= _inner.List();

    public Sensor? GetById(string id)
    {
        if (_byIdCache.TryGetValue(id, out var sensor)) return sensor;
        sensor = _inner.GetById(id);
        _byIdCache[id] = sensor;
        return sensor;
    }

    public void Add(Sensor item)
    {
        _inner.Add(item);
        Invalidate();
    }

    public void Update(Sensor item)
    {
        _inner.Update(item);
        Invalidate();
    }

    public bool Remove(string id)
    {
        var removed = _inner.Remove(id);
        if (removed) Invalidate();
        return removed;
    }

    private void Invalidate()
    {
        _listCache = null;
        _byIdCache.Clear();
    }
}
