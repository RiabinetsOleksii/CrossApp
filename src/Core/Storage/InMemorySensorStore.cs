using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemorySensorStore(IEnumerable<Sensor>? seed = null) : ISensorStore
{
    private readonly Dictionary<string, Sensor> _items =
        (seed ?? []).ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<Sensor> List() => _items.Values.ToList();

    public Sensor? GetById(string id) => _items.GetValueOrDefault(id);

    public void Add(Sensor item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.TryAdd(item.Id, item))
            throw new InvalidOperationException($"Сенсор з id={item.Id} уже існує.");
    }

    public void Update(Sensor item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Сенсор з id={item.Id} не знайдено для оновлення.");
        _items[item.Id] = item;
    }

    public bool Remove(string id) => _items.Remove(id);
}
