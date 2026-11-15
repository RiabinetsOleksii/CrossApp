using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// Keeps the Lab 5 constructor compatible; optional sink is manual DI for Lab 6.
public sealed class SensorService(ISensorStore store, INotificationSink? notificationSink = null)
{
    private readonly ISensorStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly INotificationSink? _notificationSink = notificationSink;

    public event EventHandler<SensorChangedEventArgs>? Changed;

    // Only called after a successful store operation.
    private void OnChanged(Sensor sensor, SensorChangeKind kind,
        SensorStatus? statusBefore, SensorStatus? statusAfter)
    {
        var change = new SensorChangedEventArgs(
            sensor.Id, kind, statusBefore, statusAfter, sensor.Location);
        Changed?.Invoke(this, change);
        _notificationSink?.Notify(change);
    }

    public Sensor Add(string controllerId, string model, int pingIntervalMs)
    {
        var sensor = Sensor.Create($"S-{Guid.NewGuid().ToString("N")[..8]}",
            controllerId, model, pingIntervalMs);
        _store.Add(sensor);
        OnChanged(sensor, SensorChangeKind.Added, null, sensor.Status);
        return sensor;
    }

    public void Mount(string id, string location)
    {
        var sensor = _store.GetById(id)
            ?? throw new Core.DomainException($"Сенсор з id={id} не знайдено.");
        ControllerCapacityPolicy.EnsureCanMountSensor(sensor.ControllerId, _store.List());
        var before = sensor.Status;
        sensor.MountAndActivate(location);
        _store.Update(sensor);
        OnChanged(sensor, SensorChangeKind.Mounted, before, sensor.Status);
    }

    public void SendToMaintenance(string id)
    {
        var sensor = _store.GetById(id)
            ?? throw new Core.DomainException($"Сенсор з id={id} не знайдено.");
        var before = sensor.Status;
        sensor.SendToMaintenance();
        _store.Update(sensor);
        OnChanged(sensor, SensorChangeKind.Maintenance, before, sensor.Status);
    }

    public void MarkAsBroken(string id)
    {
        var sensor = _store.GetById(id)
            ?? throw new Core.DomainException($"Сенсор з id={id} не знайдено.");
        var before = sensor.Status;
        sensor.MarkAsBroken();
        _store.Update(sensor);
        OnChanged(sensor, SensorChangeKind.Broken, before, sensor.Status);
    }

    public IReadOnlyList<Sensor> All() => _store.List();
    public Sensor? Find(string id) => _store.GetById(id);

    public bool Remove(string id)
    {
        var sensor = _store.GetById(id);
        if (sensor is null || !_store.Remove(id)) return false;
        OnChanged(sensor, SensorChangeKind.Removed, sensor.Status, null);
        return true;
    }

    // Lab 6: a delegate returns bool, so this is a query, not an event.
    public IReadOnlyList<Sensor> FindAll(Func<Sensor, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    // Lab 5 compatibility: retain the previous search API.
    public IReadOnlyList<Sensor> Search(Func<Sensor, bool> predicate) => FindAll(predicate);

    // Lab 8: async boundary; store remains sync for Lab 5-7 compatibility.
    // All records are validated before any changes reach the store.
    public async Task<int> ImportAsync(
        ISensorImporter importer, string path, CancellationToken ct = default,
        IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();
        var sensors = await importer.LoadAsync(path, ct, progress);
        int added = 0;
        foreach (var sensor in sensors)
        {
            ct.ThrowIfCancellationRequested();
            if (_store.GetById(sensor.Id) is not null) continue;
            _store.Add(sensor);
            OnChanged(sensor, SensorChangeKind.Added, null, sensor.Status);
            added++;
        }
        return added;
    }

    // Optional #1: streaming import; no complete in-memory list required.
    public async Task<int> ImportStreamAsync(
        IStreamingSensorImporter importer, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();
        int added = 0;
        await foreach (var sensor in importer.StreamAsync(path, ct).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();
            if (_store.GetById(sensor.Id) is not null) continue;
            _store.Add(sensor);
            OnChanged(sensor, SensorChangeKind.Added, null, sensor.Status);
            added++;
        }
        return added;
    }
}
