using Core.Abstractions;
using Core.Events;

// Allows both optional sink implementations to be injected as one port.
internal sealed class CompositeNotificationSink(params INotificationSink[] sinks) : INotificationSink
{
    private readonly INotificationSink[] _sinks = sinks;

    public void Notify(SensorChangedEventArgs change)
    {
        foreach (var sink in _sinks)
            sink.Notify(change);
    }
}
