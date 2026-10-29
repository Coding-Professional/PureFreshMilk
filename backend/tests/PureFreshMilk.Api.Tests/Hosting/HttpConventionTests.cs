using System.Net;
using System.Net.Http.Headers;

namespace PureFreshMilk.Api.Tests.Hosting;

public sealed class HttpConventionTests(PureFreshMilkApiFactory factory) : IClassFixture<PureFreshMilkApiFactory>
{
    [Fact]
    public async Task Unknown_api_route_returns_problem_details()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/does-not-exist");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Cors_allows_the_configured_frontend_origin()
    {
        var response = await SendPreflightAsync(PureFreshMilkApiFactory.AllowedOrigin);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal(PureFreshMilkApiFactory.AllowedOrigin, Assert.Single(origins));
    }

    [Fact]
    public async Task Cors_rejects_other_origins()
    {
        var response = await SendPreflightAsync("https://not-allowed.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public void Startup_fails_fast_without_a_connection_string()
    {
        using var misconfigured = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:PureFreshMilk", string.Empty));

        var exception = Assert.Throws<InvalidOperationException>(() => misconfigured.CreateClient());
        Assert.Contains("ConnectionStrings__PureFreshMilk", exception.Message, StringComparison.Ordinal);
    }

    private async Task<HttpResponseMessage> SendPreflightAsync(string origin)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/status");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
