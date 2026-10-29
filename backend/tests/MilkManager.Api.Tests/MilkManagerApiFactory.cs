using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MilkManager.Api.Tests;

/// <summary>
/// Hosts the API in memory. Runs in a dedicated "Testing" environment so that developer
/// user secrets and appsettings.Development.json never affect test results. The connection
/// string points at a closed local port: these tests never need a running database.
/// </summary>
public sealed class MilkManagerApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://localhost:3000";

    private const string UnreachableDatabase =
        "Host=127.0.0.1;Port=1;Database=milkmanager_tests;Username=tests;Timeout=2";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:MilkManager", UnreachableDatabase);
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
    }
}
