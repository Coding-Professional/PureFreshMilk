using System.Reflection;

namespace PureFreshMilk.Application.Status;

/// <summary>
/// Reports the running application version and the server's current time.
/// The walking-skeleton use case: it proves the Api → Application wiring
/// without touching persistence.
/// </summary>
public sealed class GetSystemStatusHandler(TimeProvider timeProvider)
{
    private static readonly string Version = ReadVersion();

    public SystemStatus Handle() => new(Version, timeProvider.GetUtcNow());

    private static string ReadVersion()
    {
        var informationalVersion = typeof(GetSystemStatusHandler).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        // Drop build metadata such as "+<commit sha>".
        return informationalVersion?.Split('+')[0] ?? "unknown";
    }
}
