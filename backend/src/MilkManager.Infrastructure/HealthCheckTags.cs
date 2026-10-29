namespace MilkManager.Infrastructure;

public static class HealthCheckTags
{
    /// <summary>Checks that must pass before the API should receive traffic.</summary>
    public const string Ready = "ready";
}
