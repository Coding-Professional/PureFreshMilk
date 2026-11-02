using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace PureFreshMilk.Api.Tests.Features.MilkRates;

public sealed class MilkRateEndpointTests(PureFreshMilkApiFactory factory) : IClassFixture<PureFreshMilkApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<HttpClient> StartWithHouseholdAsync()
    {
        factory.Database.Clear();
        var client = factory.CreateClient();
        await client.SetUpHouseholdAsync();
        return client;
    }

    [Fact]
    public async Task Listing_before_the_household_is_set_up_is_404()
    {
        factory.Database.Clear();
        using var client = factory.CreateClient();

        var problem = await (await client.GetAsync("/api/v1/milk-rates", Ct)).ReadProblemAsync(HttpStatusCode.NotFound);

        Assert.Equal("household-not-set-up", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Adding_returns_201_with_a_location_and_the_rate()
    {
        using var client = await StartWithHouseholdAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/milk-rates", new { effectiveFrom = "2026-10-01", pricePerLitrePaise = 6250 }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.ReadAsync();
        Assert.Equal($"/api/v1/milk-rates/{body.GetProperty("id").GetGuid()}", response.Headers.Location?.ToString());
        Assert.Equal("2026-10-01", body.GetProperty("effectiveFrom").GetString());
        Assert.Equal(6250, body.GetProperty("pricePerLitrePaise").GetInt64());
    }

    [Fact]
    public async Task A_rate_can_start_in_the_past()
    {
        using var client = await StartWithHouseholdAsync();

        var body = await client.AddRateAsync("2020-01-01", 4000);

        Assert.Equal("2020-01-01", body.GetProperty("effectiveFrom").GetString());
    }

    [Fact]
    public async Task The_list_is_an_object_with_items_newest_first()
    {
        using var client = await StartWithHouseholdAsync();
        await client.AddRateAsync("2026-10-15", 6400);
        await client.AddRateAsync("2026-09-01", 5500);
        await client.AddRateAsync("2026-10-01", 6000);

        var body = await (await client.GetAsync("/api/v1/milk-rates", Ct)).ReadAsync();

        var dates = body.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("effectiveFrom").GetString());
        Assert.Equal(["2026-10-15", "2026-10-01", "2026-09-01"], dates);
    }

    [Theory]
    [InlineData("""{"effectiveFrom": "2026-10-01", "pricePerLitrePaise": 0}""", "pricePerLitrePaise")]
    [InlineData("""{"effectiveFrom": "2026-10-01", "pricePerLitrePaise": -5}""", "pricePerLitrePaise")]
    [InlineData("""{"effectiveFrom": "2026-10-01", "pricePerLitrePaise": 1000001}""", "pricePerLitrePaise")]
    [InlineData("""{"effectiveFrom": "2026-10-01"}""", "pricePerLitrePaise")]
    [InlineData("""{"pricePerLitrePaise": 6250}""", "effectiveFrom")]
    public async Task Adding_invalid_input_is_400_naming_the_field(string json, string field)
    {
        using var client = await StartWithHouseholdAsync();

        var response = await client.PostAsync("/api/v1/milk-rates", Json(json), Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal("validation-failed", problem.GetProperty("code").GetString());
        Assert.NotEmpty(problem.ErrorsFor(field));
        Assert.Empty(factory.Database.MilkRates);
    }

    [Theory]
    [InlineData("""{"effectiveFrom": "yesterday", "pricePerLitrePaise": 6250}""")]
    [InlineData("""{"effectiveFrom": "2026-10-01", "pricePerLitrePaise": 62.5}""")]
    [InlineData("""{"effectiveFrom": "2026-10-01", "pricePerLitrePaise": "₹62.50"}""")]
    public async Task Adding_the_wrong_kind_of_value_is_400_problem_details(string json)
    {
        using var client = await StartWithHouseholdAsync();

        var response = await client.PostAsync("/api/v1/milk-rates", Json(json), Ct);

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_second_rate_for_the_same_day_is_409()
    {
        using var client = await StartWithHouseholdAsync();
        await client.AddRateAsync("2026-10-01", 6000);

        var response = await client.PostAsJsonAsync(
            "/api/v1/milk-rates", new { effectiveFrom = "2026-10-01", pricePerLitrePaise = 6400 }, Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("rate-date-taken", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task The_rate_in_effect_on_a_date_is_the_latest_one_that_has_started()
    {
        using var client = await StartWithHouseholdAsync();
        await client.AddRateAsync("2026-10-01", 6000);
        await client.AddRateAsync("2026-10-15", 6400);

        var before = await (await client.GetAsync("/api/v1/milk-rates/in-effect?on=2026-10-14", Ct)).ReadAsync();
        var onTheDay = await (await client.GetAsync("/api/v1/milk-rates/in-effect?on=2026-10-15", Ct)).ReadAsync();
        var today = await (await client.GetAsync("/api/v1/milk-rates/in-effect", Ct)).ReadAsync();

        Assert.Equal(6000, before.GetProperty("pricePerLitrePaise").GetInt64());
        Assert.Equal(6400, onTheDay.GetProperty("pricePerLitrePaise").GetInt64());
        Assert.Equal(6400, today.GetProperty("pricePerLitrePaise").GetInt64()); // today is 30 October, after both start dates
    }

    [Fact]
    [Trait("Invariant", "T-5")]
    public async Task With_no_rate_in_force_the_answer_is_404_not_a_zero_price()
    {
        using var client = await StartWithHouseholdAsync();
        await client.AddRateAsync("2026-10-01", 6000);

        var empty = await client.GetAsync("/api/v1/milk-rates/in-effect?on=2026-09-30", Ct);

        var problem = await empty.ReadProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal("no-rate-in-effect", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_malformed_date_in_the_query_is_400()
    {
        using var client = await StartWithHouseholdAsync();

        var response = await client.GetAsync("/api/v1/milk-rates/in-effect?on=tomorrow", Ct);

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Updating_changes_the_date_and_price()
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRateAsync("2026-10-01", 6000);
        var id = added.GetProperty("id").GetGuid();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/milk-rates/{id}", new { effectiveFrom = "2026-10-05", pricePerLitrePaise = 6400 }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsync();
        Assert.Equal(id, body.GetProperty("id").GetGuid());
        Assert.Equal("2026-10-05", body.GetProperty("effectiveFrom").GetString());
        Assert.Equal(6400, body.GetProperty("pricePerLitrePaise").GetInt64());
    }

    [Fact]
    public async Task Moving_a_rate_onto_the_day_of_another_is_409()
    {
        using var client = await StartWithHouseholdAsync();
        await client.AddRateAsync("2026-10-01", 6000);
        var other = await client.AddRateAsync("2026-10-15", 6400);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/milk-rates/{other.GetProperty("id").GetGuid()}",
            new { effectiveFrom = "2026-10-01", pricePerLitrePaise = 7000 },
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("rate-date-taken", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Updating_with_an_invalid_price_is_400_and_an_unknown_rate_is_404()
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRateAsync("2026-10-01", 6000);

        var invalid = await client.PutAsJsonAsync(
            $"/api/v1/milk-rates/{added.GetProperty("id").GetGuid()}",
            new { effectiveFrom = "2026-10-01", pricePerLitrePaise = 0 },
            Ct);
        var unknown = await client.PutAsJsonAsync(
            $"/api/v1/milk-rates/{Guid.CreateVersion7()}", new { effectiveFrom = "2026-10-01", pricePerLitrePaise = 6000 }, Ct);

        var problem = await invalid.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.NotEmpty(problem.ErrorsFor("pricePerLitrePaise"));
        var notFound = await unknown.ReadProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal("rate-not-found", notFound.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Deleting_returns_204_and_an_unknown_rate_is_404()
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRateAsync("2026-10-01", 6000);

        var deleted = await client.DeleteAsync($"/api/v1/milk-rates/{added.GetProperty("id").GetGuid()}", Ct);
        var again = await client.DeleteAsync($"/api/v1/milk-rates/{added.GetProperty("id").GetGuid()}", Ct);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(factory.Database.MilkRates);
        await again.ReadProblemAsync(HttpStatusCode.NotFound);
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");
}
