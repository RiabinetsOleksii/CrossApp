using Core.Abstractions;
using Core.Events;

// Output stays in Cli, not Core.
internal sealed class ConsoleSink : INotificationSink
{
    public void Notify(SensorChangedEventArgs change) =>
        Console.WriteLine($"[SINK/CONSOLE] {change.Kind} {change.Id} -> {change.StatusAfter}");
}
