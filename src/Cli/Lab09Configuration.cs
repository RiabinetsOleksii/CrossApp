using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

internal sealed record Lab09Settings(
    string EnvironmentName,
    string LoggingLevel,
    string DataFile,
    bool HasApiKey,
    int ApiKeyLength);

internal static class Lab09Configuration
{
    public static Lab09Settings Load(string[] args)
    {
        var envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT");
        if (string.IsNullOrWhiteSpace(envName)) envName = "Development";

        // Use the compiled application's directory for configuration: independent of cwd.
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_");

        // Existing --lab04/--lab08/--file/--data and positional commands must
        // never be passed to AddCommandLine, which expects config key/value pairs.
        var configArgs = args.Where(a =>
            a.StartsWith("--Storage:", StringComparison.OrdinalIgnoreCase) ||
            a.StartsWith("--Logging:", StringComparison.OrdinalIgnoreCase) ||
            a.StartsWith("--EnvironmentName=", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (configArgs.Length != 0) builder.AddCommandLine(configArgs);

        var configuration = builder.Build();
        var options = configuration.Get<AppOptions>() ?? new AppOptions();

        // Backwards-compatible simple override wins over nested Storage:DataPath.
        var simplePath = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        if (!string.IsNullOrWhiteSpace(simplePath))
            options.Storage.DataPath = simplePath;

        // Project root is located from executable output; no hardcoded OS paths.
        // For published copies without CrossApp.slnx it falls back to base directory.
        string dataFile = options.Storage.ResolveFullPath(ProjectPaths.FindRepositoryRoot());
        var secret = configuration["Integration:ApiKey"];
        return new Lab09Settings(
            envName,
            options.Logging.MinimumLevel,
            dataFile,
            !string.IsNullOrEmpty(secret),
            secret?.Length ?? 0);
    }
}
