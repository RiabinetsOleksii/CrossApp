using Core.Abstractions;
using Core.Events;

// Output stays in Cli, not Core.
internal sealed class FileSink(string path) : INotificationSink
{
    private readonly string _path = Path.GetFullPath(path);

    public void Notify(SensorChangedEventArgs change)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.AppendAllText(_path,
            $"{change.At:HH:mm:ss} {change.Kind} {change.Id} -> {change.StatusAfter}" +
            Environment.NewLine);
    }
}
