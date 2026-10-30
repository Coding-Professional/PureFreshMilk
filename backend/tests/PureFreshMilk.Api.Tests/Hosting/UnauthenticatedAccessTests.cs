using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;

namespace PureFreshMilk.Api.Tests.Hosting;

/// <summary>
/// The data endpoints read and write a household without a login, so they exist only in Development and
/// Testing, or when <c>Access:AllowUnauthenticated</c> is switched on explicitly (ADR 0004).
/// </summary>
public sealed class UnauthenticatedAccessTests(PureFreshMilkApiFactory factory) : IClassFixture<PureFreshMilkApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly string[] DataRoutes =
    [
        "/api/v1/household",
        "/api/v1/recipients",
        "/api/v1/milk-rates",
    ];

    private HttpClient ClientFor(string environment, string? allowUnauthenticated = null)
    {
        // Each derived host gets its own in-memory database, so there is nothing to clear.
        return factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                if (allowUnauthenticated is not null)
                {
                    builder.UseSetting("Access:AllowUnauthenticated", allowUnauthenticated);
                }
            })
            .CreateClient();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Outside_development_and_testing_the_data_endpoints_are_not_mapped(string environment)
    {
        using var client = ClientFor(environment);

        foreach (var route in DataRoutes)
        {
            var response = await client.GetAsync(route, Ct);
            var problem = await response.ReadProblemAsync(HttpStatusCode.NotFound);

            // A route that does not exist, not the "household not set up" answer of a route that does.
            Assert.False(problem.TryGetProperty("code", out _), route);
        }

        var write = await client.PutAsJsonAsync("/api/v1/household", new { name = "Home" }, Ct);
        Assert.Equal(HttpStatusCode.NotFound, write.StatusCode);
    }

    [Fact]
    public async Task Outside_development_and_testing_the_status_and_health_endpoints_still_work()
    {
        using var client = ClientFor("Production");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/status", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live", Ct)).StatusCode);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("")]
    [InlineData("maybe")]
    public async Task Anything_but_true_leaves_the_data_endpoints_off(string setting)
    {
        using var client = ClientFor("Production", setting);

        var response = await client.PutAsJsonAsync("/api/v1/household", new { name = "Home" }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task The_setting_turns_the_data_endpoints_on_outside_development()
    {
        using var client = ClientFor("Production", "true");

        var response = await client.PutAsJsonAsync("/api/v1/household", new { name = "Home" }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public async Task In_development_and_testing_the_data_endpoints_are_mapped(string environment)
    {
        using var client = ClientFor(environment);

        var response = await client.PutAsJsonAsync("/api/v1/household", new { name = "Home" }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
