using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MilkManager.Api.Tests.Features;

public sealed class StatusEndpointTests(MilkManagerApiFactory factory) : IClassFixture<MilkManagerApiFactory>
{
    [Fact]
    public async Task Get_status_returns_service_version_and_server_time()
    {
        var now = new DateTimeOffset(2026, 1, 15, 6, 30, 0, TimeSpan.Zero);
        using var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(now))))
            .CreateClient();

        var response = await client.GetAsync("/api/v1/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = body.RootElement;
        Assert.Equal("milk-manager-api", root.GetProperty("service").GetString());
        Assert.Matches(@"^\d+\.\d+\.\d+", root.GetProperty("version").GetString());
        Assert.Equal(now, root.GetProperty("serverTime").GetDateTimeOffset());
    }
}
