using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PureFreshMilk.TestSupport;

namespace PureFreshMilk.Api.Tests.Features.Households;

public sealed class HouseholdEndpointTests(PureFreshMilkApiFactory factory) : IClassFixture<PureFreshMilkApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private HttpClient Start()
    {
        factory.Database.Clear();
        return factory.CreateClient();
    }

    [Fact]
    public async Task Reading_before_set_up_is_404_with_a_code_clients_can_branch_on()
    {
        using var client = Start();

        var response = await client.GetAsync("/api/v1/household", Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal("household-not-set-up", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Setting_up_creates_the_household_then_updates_it()
    {
        using var client = Start();

        var created = await client.SetUpHouseholdAsync();

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("/api/v1/household", created.Headers.Location?.ToString());
        Assert.Equal("application/json", created.Content.Headers.ContentType?.MediaType);
        var body = await created.ReadAsync();
        Assert.Equal("Example household", body.GetProperty("name").GetString());
        Assert.Equal("Asia/Kolkata", body.GetProperty("timeZoneId").GetString());
        Assert.Equal("2026-10-30", body.GetProperty("today").GetString());
        Assert.True(body.GetProperty("id").TryGetGuid(out var id));

        var updated = await client.PutAsJsonAsync("/api/v1/household", new { name = "Renamed" }, Ct);

        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedBody = await updated.ReadAsync();
        Assert.Equal(id, updatedBody.GetProperty("id").GetGuid());
        Assert.Equal("Renamed", updatedBody.GetProperty("name").GetString());
        Assert.Equal("Asia/Kolkata", updatedBody.GetProperty("timeZoneId").GetString());

        var read = await (await client.GetAsync("/api/v1/household", Ct)).ReadAsync();
        Assert.Equal("Renamed", read.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Today_is_the_date_in_the_household_time_zone()
    {
        factory.Database.Clear();
        var atMidnightInKolkata = new DateTimeOffset(2026, 10, 30, 18, 30, 0, TimeSpan.Zero);
        using var client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(atMidnightInKolkata))))
            .CreateClient();

        var body = await (await client.SetUpHouseholdAsync()).ReadAsync();

        Assert.Equal("2026-10-31", body.GetProperty("today").GetString());
    }

    [Theory]
    [InlineData("{}", "name")]
    [InlineData("""{"name": "   "}""", "name")]
    [InlineData("""{"name": "Home", "timeZoneId": "Mars/Olympus_Mons"}""", "timeZoneId")]
    [InlineData("""{"name": "Home", "timeZoneId": "India Standard Time"}""", "timeZoneId")]
    public async Task Invalid_input_is_400_naming_the_field(string json, string field)
    {
        using var client = Start();

        var response = await client.PutAsync("/api/v1/household", Json(json), Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal("validation-failed", problem.GetProperty("code").GetString());
        Assert.NotEmpty(problem.ErrorsFor(field));
        Assert.Empty(factory.Database.Households);
    }

    [Fact]
    public async Task Malformed_json_is_400_problem_details()
    {
        using var client = Start();

        var response = await client.PutAsync("/api/v1/household", Json("{ not json"), Ct);

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_missing_body_is_400_problem_details()
    {
        using var client = Start();

        var response = await client.PutAsync("/api/v1/household", content: null, Ct);

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    private static StringContent Json(string json) => new(json, System.Text.Encoding.UTF8, "application/json");
}
