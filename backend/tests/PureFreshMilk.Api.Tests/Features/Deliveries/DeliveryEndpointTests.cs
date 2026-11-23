using System.Net;
using System.Net.Http.Json;

namespace PureFreshMilk.Api.Tests.Features.Deliveries;

public sealed class DeliveryEndpointTests(PureFreshMilkApiFactory factory) : IClassFixture<PureFreshMilkApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_delivery_can_be_recorded_replaced_and_cleared_through_http()
    {
        factory.Database.Clear();
        using var client = factory.CreateClient();
        await client.SetUpHouseholdAsync();
        var recipient = await client.AddRecipientAsync("Recipient A", 750);
        var id = recipient.GetProperty("id").GetGuid();
        var path = $"/api/v1/recipients/{id}/deliveries/today";

        var empty = await (await client.GetAsync("/api/v1/day-sheets/today", Ct)).ReadAsync();
        Assert.Equal("notRecorded", empty.GetProperty("rows")[0].GetProperty("status").GetString());

        var created = await client.PutAsJsonAsync(path, new { status = "delivered" }, Ct);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(750, (await created.ReadAsync()).GetProperty("deliveredMl").GetInt32());

        var replaced = await client.PutAsJsonAsync(path, new { status = "missed" }, Ct);
        Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);
        Assert.Equal(0, (await replaced.ReadAsync()).GetProperty("deliveredMl").GetInt32());

        var invalid = await client.PutAsJsonAsync(path, new { status = "missed", deliveredMl = 500 }, Ct);
        var problem = await invalid.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.NotEmpty(problem.ErrorsFor("deliveredMl"));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(path, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync(path, Ct)).StatusCode);
        Assert.Equal("notRecorded", (await (await client.GetAsync("/api/v1/day-sheets/today", Ct)).ReadAsync())
            .GetProperty("rows")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Mark_remaining_and_history_return_server_totals()
    {
        factory.Database.Clear();
        using var client = factory.CreateClient();
        await client.SetUpHouseholdAsync();
        await client.AddRecipientAsync("Recipient A", 750, "2026-10-29");
        await client.AddRecipientAsync("Recipient B", 500, "2026-10-29");

        var filled = await (await client.PostAsync("/api/v1/day-sheets/today/deliver-remaining", null, Ct)).ReadAsync();
        Assert.Equal(1250, filled.GetProperty("totals").GetProperty("deliveredMl").GetInt32());
        Assert.Equal(2, filled.GetProperty("totals").GetProperty("delivered").GetInt32());

        var history = await (await client.GetAsync("/api/v1/day-summaries?from=2026-10-29&to=2026-10-30", Ct)).ReadAsync();
        var items = history.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal("2026-10-30", items[0].GetProperty("date").GetString());
        Assert.Equal(1250, items[0].GetProperty("totals").GetProperty("deliveredMl").GetInt32());
        Assert.Equal(2, items[1].GetProperty("totals").GetProperty("notRecorded").GetInt32());
    }

    [Fact]
    public async Task Future_day_is_a_400_with_a_stable_code()
    {
        factory.Database.Clear();
        using var client = factory.CreateClient();
        await client.SetUpHouseholdAsync();

        var problem = await (await client.GetAsync("/api/v1/day-sheets/2026-10-31", Ct))
            .ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal("future-date", problem.GetProperty("code").GetString());
        Assert.NotEmpty(problem.ErrorsFor("date"));
    }
}
