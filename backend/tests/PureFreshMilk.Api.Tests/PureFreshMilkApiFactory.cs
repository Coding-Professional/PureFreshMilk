using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PureFreshMilk.TestSupport;

namespace PureFreshMilk.Api.Tests;

/// <summary>
/// Hosts the API in memory. Runs in a dedicated "Testing" environment so that developer
/// user secrets and appsettings.Development.json never affect test results. The connection
/// string points at a closed local port: these tests never need a running database. The
/// repositories are replaced by in-memory ones, so the endpoints can be tested end to end
/// without PostgreSQL, and the clock stands still at <see cref="StartTime"/>.
/// </summary>
public sealed class PureFreshMilkApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://localhost:3000";

    /// <summary>06:00 UTC on 30 October 2026: 11:30 on the same day in Asia/Kolkata.</summary>
    public static readonly DateTimeOffset StartTime = new(2026, 10, 30, 6, 0, 0, TimeSpan.Zero);

    private const string UnreachableDatabase =
        "Host=127.0.0.1;Port=1;Database=purefreshmilk_tests;Username=tests;Timeout=2";

    /// <summary>The in-memory database behind the endpoints. Clear it at the start of a test that shares this host.</summary>
    public InMemoryDatabase Database => Services.GetRequiredService<InMemoryDatabase>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:PureFreshMilk", UnreachableDatabase);
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(StartTime));
            services.UseInMemoryPersistence();
        });
    }
}
