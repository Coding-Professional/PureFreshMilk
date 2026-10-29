using System.Net;

namespace PureFreshMilk.Api.Tests.Hosting;

public sealed class HealthEndpointTests(PureFreshMilkApiFactory factory) : IClassFixture<PureFreshMilkApiFactory>
{
    [Fact]
    public async Task Liveness_is_healthy_without_a_database()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Readiness_is_unhealthy_when_the_database_is_unreachable()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
