using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Deliveries;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Application.Tests.Deliveries;

public sealed class DeliveryHandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("Invariant", "L-7,L-8,L-13,L-14,R-10")]
    public async Task Day_sheet_tracks_record_replace_clear_and_remaining_without_changing_existing_quantities()
    {
        var app = await TestHousehold.SetUpAsync();
        var first = await app.AddRecipientAsync("Recipient A", 750);
        await app.AddRecipientAsync("Recipient B", 500);
        var empty = await app.GetDaySheet.HandleAsync("today", Ct);
        Assert.Equal(2, empty.Totals.NotRecorded);
        Assert.Equal(1250, empty.Totals.ExpectedMl);

        var recorded = await app.RecordDelivery.HandleAsync(new(first.Id, "today", "delivered", null), Ct);
        Assert.True(recorded.Created);
        Assert.Equal(750, recorded.Delivery.DeliveredQuantity.Millilitres);
        var replaced = await app.RecordDelivery.HandleAsync(new(first.Id, "today", "missed", null), Ct);
        Assert.False(replaced.Created);
        Assert.Single(app.Database.Deliveries);
        Assert.Equal(0, replaced.Delivery.DeliveredQuantity.Millilitres);

        var filled = await app.DeliverRemaining.HandleAsync("today", Ct);
        Assert.Equal(1, filled.Totals.Delivered);
        Assert.Equal(1, filled.Totals.Missed);
        Assert.Equal(500, filled.Totals.DeliveredMl);
        Assert.Equal(2, app.Database.Deliveries.Count);

        await app.UpdateRecipient.HandleAsync(new UpdateRecipientCommand(first.Id, "Recipient A", 1000, first.StartsOn), Ct);
        Assert.Equal(0, app.Database.Deliveries.Single(delivery => delivery.RecipientId.Value == first.Id).DeliveredQuantity.Millilitres);
        await app.ClearDelivery.HandleAsync(first.Id, "today", Ct);
        await app.ClearDelivery.HandleAsync(first.Id, "today", Ct);
        Assert.Equal(1, (await app.GetDaySheet.HandleAsync("today", Ct)).Totals.NotRecorded);
    }

    [Fact]
    [Trait("Invariant", "L-5,L-6,L-15,R-6,R-7")]
    public async Task Date_and_recipient_boundaries_are_enforced()
    {
        var app = await TestHousehold.SetUpAsync();
        var first = await app.AddRecipientAsync("Recipient A", 750, new DateOnly(2026, 10, 1));
        await Assert.ThrowsAsync<FutureDateException>(() => app.GetDaySheet.HandleAsync("2026-10-31", Ct));
        await Assert.ThrowsAsync<ConflictException>(() => app.RecordDelivery.HandleAsync(
            new(first.Id, "2026-09-30", "delivered", null), Ct));
        await app.RecordDelivery.HandleAsync(new(first.Id, "2026-10-15", "delivered", null), Ct);
        await Assert.ThrowsAsync<ConflictException>(() => app.UpdateRecipient.HandleAsync(
            new(first.Id, "Recipient A", 750, new DateOnly(2026, 10, 16)), Ct));
        await Assert.ThrowsAsync<ConflictException>(() => app.ArchiveRecipient.HandleAsync(
            new(first.Id, new DateOnly(2026, 10, 15)), Ct));
        await Assert.ThrowsAsync<DomainException>(() => app.ListDaySummaries.HandleAsync(
            new DateOnly(2025, 1, 1), new DateOnly(2026, 10, 30), Ct));
    }
}
