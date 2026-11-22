namespace Core.Configuration;

// Lab 9: POCO options. Core does not depend on configuration packages.
public sealed class AppOptions
{
    public StorageOptions Storage { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public string EnvironmentName { get; set; } = "Development";
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; set; } = "Information";
}

public sealed class StorageOptions
{
    public string DataPath { get; set; } = "data";
    public string FileName { get; set; } = "sensors.json";

    // baseDir is deliberately supplied by the caller, not taken from cwd.
    public string ResolveFullPath(string baseDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(DataPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(FileName);

        if (Path.IsPathRooted(FileName) ||
            !string.Equals(Path.GetFileName(FileName), FileName, StringComparison.Ordinal) ||
            FileName is "." or "..")
            throw new ArgumentException("Storage:FileName має бути тільки назвою файлу, без каталогів.");

        var root = Path.IsPathRooted(DataPath)
            ? Path.GetFullPath(DataPath)
            : Path.GetFullPath(Path.Combine(baseDir, DataPath));
        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}
