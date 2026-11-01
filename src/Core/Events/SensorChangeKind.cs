namespace Core.Events;

// Types of successfully completed sensor operations.
public enum SensorChangeKind
{
    Added,
    Mounted,
    Maintenance,
    Broken,
    Removed
}
