using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace PureFreshMilk.Api.Tests.Features.Recipients;

public sealed class RecipientEndpointTests(PureFreshMilkApiFactory factory) : IClassFixture<PureFreshMilkApiFactory>
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

        var problem = await (await client.GetAsync("/api/v1/recipients", Ct)).ReadProblemAsync(HttpStatusCode.NotFound);

        Assert.Equal("household-not-set-up", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Adding_returns_201_with_a_location_and_the_recipient()
    {
        using var client = await StartWithHouseholdAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/recipients", new { name = "  Recipient A ", expectedMl = 1000 }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.ReadAsync();
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal($"/api/v1/recipients/{id}", response.Headers.Location?.ToString());
        Assert.Equal("Recipient A", body.GetProperty("name").GetString());
        Assert.Equal(1000, body.GetProperty("expectedMl").GetInt32());
        Assert.Equal("2026-10-30", body.GetProperty("startsOn").GetString()); // defaults to today
        Assert.Equal(System.Text.Json.JsonValueKind.Null, body.GetProperty("archivedFrom").ValueKind);
    }

    [Fact]
    public async Task The_list_is_an_object_with_items_ordered_by_name()
    {
        using var client = await StartWithHouseholdAsync();
        await client.AddRecipientAsync("Recipient B", 500, "2026-10-01");
        await client.AddRecipientAsync("Recipient A", 1000, "2026-10-01");

        var body = await (await client.GetAsync("/api/v1/recipients", Ct)).ReadAsync();

        var names = body.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString());
        Assert.Equal(["Recipient A", "Recipient B"], names);
    }

    [Fact]
    public async Task An_empty_household_lists_no_recipients()
    {
        using var client = await StartWithHouseholdAsync();

        var body = await (await client.GetAsync("/api/v1/recipients", Ct)).ReadAsync();

        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
    }

    [Theory]
    [InlineData("""{"expectedMl": 500}""", "name")]
    [InlineData("""{"name": "  ", "expectedMl": 500}""", "name")]
    [InlineData("""{"name": "Recipient A"}""", "expectedMl")]
    [InlineData("""{"name": "Recipient A", "expectedMl": 0}""", "expectedMl")]
    [InlineData("""{"name": "Recipient A", "expectedMl": -250}""", "expectedMl")]
    [InlineData("""{"name": "Recipient A", "expectedMl": 20001}""", "expectedMl")]
    public async Task Adding_invalid_input_is_400_naming_the_field(string json, string field)
    {
        using var client = await StartWithHouseholdAsync();

        var response = await client.PostAsync("/api/v1/recipients", Json(json), Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal("validation-failed", problem.GetProperty("code").GetString());
        Assert.NotEmpty(problem.ErrorsFor(field));
        Assert.Empty(factory.Database.Recipients);
    }

    [Theory]
    [InlineData("""{"name": "Recipient A", "expectedMl": "plenty"}""")]
    [InlineData("""{"name": "Recipient A", "expectedMl": 500, "startsOn": "30/10/2026"}""")]
    [InlineData("""{"name": "Recipient A", "expectedMl": 500.5}""")]
    [InlineData("{ not json")]
    public async Task Adding_the_wrong_kind_of_value_is_400_problem_details(string json)
    {
        using var client = await StartWithHouseholdAsync();

        var response = await client.PostAsync("/api/v1/recipients", Json(json), Ct);

        await response.ReadProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Adding_a_duplicate_name_is_409_whatever_its_case()
    {
        using var client = await StartWithHouseholdAsync();
        await client.AddRecipientAsync("Recipient A");

        var response = await client.PostAsJsonAsync("/api/v1/recipients", new { name = "recipient a", expectedMl = 500 }, Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("recipient-name-taken", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_recipient_can_be_read_by_id()
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRecipientAsync("Recipient A", 750, "2026-10-01");

        var body = await (await client.GetAsync($"/api/v1/recipients/{added.GetProperty("id").GetGuid()}", Ct)).ReadAsync();

        Assert.Equal("Recipient A", body.GetProperty("name").GetString());
        Assert.Equal(750, body.GetProperty("expectedMl").GetInt32());
        Assert.Equal("2026-10-01", body.GetProperty("startsOn").GetString());
    }

    [Fact]
    public async Task An_unknown_recipient_is_404_and_a_malformed_id_is_not_a_route()
    {
        using var client = await StartWithHouseholdAsync();

        var unknown = await client.GetAsync($"/api/v1/recipients/{Guid.CreateVersion7()}", Ct);
        var problem = await unknown.ReadProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal("recipient-not-found", problem.GetProperty("code").GetString());

        var malformed = await client.GetAsync("/api/v1/recipients/not-a-guid", Ct);
        Assert.Equal(HttpStatusCode.NotFound, malformed.StatusCode);
    }

    [Fact]
    public async Task Updating_replaces_the_editable_fields()
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRecipientAsync("Recipient A", 1000, "2026-10-01");
        var id = added.GetProperty("id").GetGuid();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/recipients/{id}", new { name = "Recipient Z", expectedMl = 500, startsOn = "2026-09-15" }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadAsync();
        Assert.Equal(id, body.GetProperty("id").GetGuid());
        Assert.Equal("Recipient Z", body.GetProperty("name").GetString());
        Assert.Equal(500, body.GetProperty("expectedMl").GetInt32());
        Assert.Equal("2026-09-15", body.GetProperty("startsOn").GetString());
    }

    [Theory]
    [InlineData("""{"name": "", "expectedMl": 500, "startsOn": "2026-10-01"}""", "name")]
    [InlineData("""{"name": "Recipient A", "expectedMl": 0, "startsOn": "2026-10-01"}""", "expectedMl")]
    [InlineData("""{"name": "Recipient A", "expectedMl": 500}""", "startsOn")]
    public async Task Updating_with_invalid_input_is_400_naming_the_field(string json, string field)
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRecipientAsync("Recipient A", 1000, "2026-10-01");

        var response = await client.PutAsync($"/api/v1/recipients/{added.GetProperty("id").GetGuid()}", Json(json), Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.NotEmpty(problem.ErrorsFor(field));
    }

    [Fact]
    public async Task Renaming_onto_another_active_recipient_is_409()
    {
        using var client = await StartWithHouseholdAsync();
        await client.AddRecipientAsync("Recipient A");
        var other = await client.AddRecipientAsync("Recipient B");

        var response = await client.PutAsJsonAsync(
            $"/api/v1/recipients/{other.GetProperty("id").GetGuid()}",
            new { name = "Recipient A", expectedMl = 500, startsOn = "2026-10-30" },
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("recipient-name-taken", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Updating_an_unknown_recipient_is_404()
    {
        using var client = await StartWithHouseholdAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/v1/recipients/{Guid.CreateVersion7()}", new { name = "Recipient A", expectedMl = 500, startsOn = "2026-10-30" }, Ct);

        await response.ReadProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Archiving_without_a_body_archives_from_today()
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRecipientAsync("Recipient A", startsOn: "2026-10-01");

        var response = await client.PostAsync($"/api/v1/recipients/{added.GetProperty("id").GetGuid()}/archive", content: null, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2026-10-30", (await response.ReadAsync()).GetProperty("archivedFrom").GetString());
    }

    [Fact]
    public async Task Archiving_can_name_the_day()
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRecipientAsync("Recipient A", startsOn: "2026-10-01");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/recipients/{added.GetProperty("id").GetGuid()}/archive", new { archivedFrom = "2026-11-15" }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2026-11-15", (await response.ReadAsync()).GetProperty("archivedFrom").GetString());
    }

    [Fact]
    public async Task Archiving_on_or_before_the_start_date_is_400_naming_the_field()
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRecipientAsync("Recipient A", startsOn: "2026-10-01");

        var response = await client.PostAsJsonAsync(
            $"/api/v1/recipients/{added.GetProperty("id").GetGuid()}/archive", new { archivedFrom = "2026-10-01" }, Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.NotEmpty(problem.ErrorsFor("archivedFrom"));
    }

    [Fact]
    public async Task Archiving_twice_is_409()
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRecipientAsync("Recipient A", startsOn: "2026-10-01");
        var url = $"/api/v1/recipients/{added.GetProperty("id").GetGuid()}/archive";
        await client.PostAsync(url, content: null, Ct);

        var second = await client.PostAsync(url, content: null, Ct);

        var problem = await second.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("recipient-already-archived", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Archived_recipients_are_hidden_unless_asked_for_and_can_be_restored()
    {
        using var client = await StartWithHouseholdAsync();
        var gone = await client.AddRecipientAsync("Recipient A", startsOn: "2026-10-01");
        await client.AddRecipientAsync("Recipient B", startsOn: "2026-10-01");
        var id = gone.GetProperty("id").GetGuid();
        await client.PostAsync($"/api/v1/recipients/{id}/archive", content: null, Ct);

        var visible = await (await client.GetAsync("/api/v1/recipients", Ct)).ReadAsync();
        var everyone = await (await client.GetAsync("/api/v1/recipients?includeArchived=true", Ct)).ReadAsync();
        Assert.Equal(1, visible.GetProperty("items").GetArrayLength());
        Assert.Equal(2, everyone.GetProperty("items").GetArrayLength());

        var restored = await client.PostAsync($"/api/v1/recipients/{id}/unarchive", content: null, Ct);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await restored.ReadAsync()).GetProperty("archivedFrom").ValueKind);
        var again = await (await client.GetAsync("/api/v1/recipients", Ct)).ReadAsync();
        Assert.Equal(2, again.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Restoring_into_a_name_that_is_now_taken_is_409()
    {
        using var client = await StartWithHouseholdAsync();
        var first = await client.AddRecipientAsync("Recipient A", startsOn: "2026-10-01");
        var id = first.GetProperty("id").GetGuid();
        await client.PostAsync($"/api/v1/recipients/{id}/archive", content: null, Ct);
        await client.AddRecipientAsync("Recipient A");

        var response = await client.PostAsync($"/api/v1/recipients/{id}/unarchive", content: null, Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("recipient-name-taken", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Archiving_or_restoring_an_unknown_recipient_is_404()
    {
        using var client = await StartWithHouseholdAsync();

        var archive = await client.PostAsync($"/api/v1/recipients/{Guid.CreateVersion7()}/archive", content: null, Ct);
        var unarchive = await client.PostAsync($"/api/v1/recipients/{Guid.CreateVersion7()}/unarchive", content: null, Ct);

        await archive.ReadProblemAsync(HttpStatusCode.NotFound);
        await unarchive.ReadProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task There_is_no_way_to_delete_a_recipient()
    {
        using var client = await StartWithHouseholdAsync();
        var added = await client.AddRecipientAsync("Recipient A");

        var response = await client.DeleteAsync($"/api/v1/recipients/{added.GetProperty("id").GetGuid()}", Ct);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Single(factory.Database.Recipients);
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");
}
