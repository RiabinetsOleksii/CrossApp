using Core.Events;

namespace Core.Abstractions;

// Optional task 1: output-agnostic notification port.
public interface INotificationSink
{
    void Notify(SensorChangedEventArgs change);
}
