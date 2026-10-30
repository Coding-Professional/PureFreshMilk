namespace PureFreshMilk.Api.Hosting;

/// <summary>
/// Until authentication exists the data endpoints read and write a household without a login, so they are
/// switched on only where that is safe: Development and Testing, or when <c>Access:AllowUnauthenticated</c>
/// (environment variable <c>Access__AllowUnauthenticated</c>) is set to <c>true</c> on purpose (ADR 0004).
/// </summary>
internal static class UnauthenticatedAccess
{
    public const string SettingName = "Access:AllowUnauthenticated";

    public const string TestingEnvironment = "Testing";

    /// <summary>Whether the data endpoints are allowed to run without a login in this environment.</summary>
    public static bool IsAllowed(IHostEnvironment environment, IConfiguration configuration) =>
        IsLocalEnvironment(environment) || IsSwitchedOn(configuration);

    /// <summary>Whether the data endpoints are on only because the setting was turned on explicitly.</summary>
    public static bool IsExplicitOptIn(IHostEnvironment environment, IConfiguration configuration) =>
        !IsLocalEnvironment(environment) && IsSwitchedOn(configuration);

    private static bool IsLocalEnvironment(IHostEnvironment environment) =>
        environment.IsDevelopment() || environment.IsEnvironment(TestingEnvironment);

    // Only an exact "true" counts. An empty or misspelled value leaves the endpoints off instead of
    // stopping the API from starting.
    private static bool IsSwitchedOn(IConfiguration configuration) =>
        bool.TryParse(configuration[SettingName], out var switchedOn) && switchedOn;
}
