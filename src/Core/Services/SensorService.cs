using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

// Domain operations know ISensorStore only; no File, JSON or Console here.
public sealed class SensorService(ISensorStore store)
{
    private readonly ISensorStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public Sensor Add(string controllerId, string model, int pingIntervalMs)
    {
        var sensor = Sensor.Create($"S-{Guid.NewGuid().ToString("N")[..8]}",
            controllerId, model, pingIntervalMs);
        _store.Add(sensor);
        return sensor;
    }

    public void Mount(string id, string location)
    {
        var sensor = _store.GetById(id)
            ?? throw new InvalidOperationException($"Сенсор з id={id} не знайдено.");
        ControllerCapacityPolicy.EnsureCanMountSensor(sensor.ControllerId, _store.List());
        sensor.MountAndActivate(location);
        _store.Update(sensor);
    }

    public void SendToMaintenance(string id)
    {
        var sensor = _store.GetById(id)
            ?? throw new InvalidOperationException($"Сенсор з id={id} не знайдено.");
        sensor.SendToMaintenance();
        _store.Update(sensor);
    }

    public void MarkAsBroken(string id)
    {
        var sensor = _store.GetById(id)
            ?? throw new InvalidOperationException($"Сенсор з id={id} не знайдено.");
        sensor.MarkAsBroken();
        _store.Update(sensor);
    }

    public IReadOnlyList<Sensor> All() => _store.List();
    public Sensor? Find(string id) => _store.GetById(id);
    public bool Remove(string id) => _store.Remove(id);

    // Optional task 2: predicate-based search, useful for labs 6-7.
    public IReadOnlyList<Sensor> Search(Func<Sensor, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }
}
