using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Rates;
using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Application.Tests.Rates;

public sealed class MilkRateHandlerTests
{
    private static readonly DateOnly October1 = new(2026, 10, 1);
    private static readonly DateOnly October15 = new(2026, 10, 15);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Adding_stores_the_rate()
    {
        var app = await TestHousehold.SetUpAsync();

        var rate = await app.AddRateAsync(October1, 6250);

        Assert.Equal(new MilkRateDetails(rate.Id, October1, 6250), rate);
        Assert.Equal(rate.Id, Assert.Single(app.Database.MilkRates).Id.Value);
        Assert.Equal(1, app.Database.SaveCount);
    }

    [Fact]
    [Trait("Invariant", "T-4")]
    public async Task A_rate_can_be_added_for_a_day_in_the_past()
    {
        var app = await TestHousehold.SetUpAsync();

        var rate = await app.AddRateAsync(new DateOnly(2020, 1, 1), 4000);

        Assert.Equal(new DateOnly(2020, 1, 1), rate.EffectiveFrom);
    }

    [Fact]
    [Trait("Invariant", "T-3")]
    public async Task A_second_rate_for_the_same_day_is_a_conflict()
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRateAsync(October1, 6250);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => app.AddRateAsync(October1, 6400));

        Assert.Equal(ErrorCodes.RateDateTaken, exception.Code);
        Assert.Single(app.Database.MilkRates);
    }

    [Theory]
    [Trait("Invariant", "T-1")]
    [InlineData(0)]
    [InlineData(-100)]
    [InlineData(1_000_001)]
    public async Task An_invalid_price_is_rejected_with_the_field_and_nothing_is_stored(long paise)
    {
        var app = await TestHousehold.SetUpAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => app.AddRateAsync(October1, paise));

        Assert.Equal("pricePerLitrePaise", exception.Field);
        Assert.Empty(app.Database.MilkRates);
        Assert.Equal(0, app.Database.SaveCount);
    }

    [Fact]
    [Trait("Invariant", "T-1")]
    public async Task A_missing_date_is_rejected()
    {
        var app = await TestHousehold.SetUpAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(() => app.AddRateAsync(default, 6250));

        Assert.Equal("effectiveFrom", exception.Field);
    }

    [Fact]
    public async Task Adding_before_the_household_is_set_up_is_not_found()
    {
        var app = new TestHousehold();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => app.AddRateAsync(October1, 6250));

        Assert.Equal(ErrorCodes.HouseholdNotSetUp, exception.Code);
    }

    [Fact]
    public async Task The_list_shows_the_newest_effective_date_first()
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRateAsync(October15, 6400);
        await app.AddRateAsync(new DateOnly(2026, 9, 1), 5500); // added last, but back-dated
        await app.AddRateAsync(October1, 6000);

        var list = await app.ListRates.HandleAsync(Ct);

        Assert.Equal([October15, October1, new DateOnly(2026, 9, 1)], list.Select(rate => rate.EffectiveFrom));
    }

    [Fact]
    public async Task The_list_is_empty_before_any_rate_is_added()
    {
        var app = await TestHousehold.SetUpAsync();

        Assert.Empty(await app.ListRates.HandleAsync(Ct));
    }

    [Fact]
    [Trait("Invariant", "T-5")]
    public async Task With_no_rate_in_force_the_answer_is_none_not_zero()
    {
        var app = await TestHousehold.SetUpAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => app.GetRateInEffect.HandleAsync(null, Ct));

        Assert.Equal(ErrorCodes.NoRateInEffect, exception.Code);
    }

    [Fact]
    [Trait("Invariant", "T-5")]
    public async Task A_day_before_the_first_rate_has_no_price()
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRateAsync(October1, 6000);

        await Assert.ThrowsAsync<NotFoundException>(() => app.GetRateInEffect.HandleAsync(new DateOnly(2026, 9, 30), Ct));
    }

    [Theory]
    [InlineData("2026-10-01", 6000)]
    [InlineData("2026-10-14", 6000)]
    [InlineData("2026-10-15", 6400)]
    [InlineData("2027-01-01", 6400)]
    public async Task The_rate_in_force_is_the_latest_one_starting_on_or_before_the_day(string date, long expectedPaise)
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRateAsync(October15, 6400);
        await app.AddRateAsync(October1, 6000);

        var rate = await app.GetRateInEffect.HandleAsync(
            DateOnly.Parse(date, System.Globalization.CultureInfo.InvariantCulture), Ct);

        Assert.Equal(expectedPaise, rate.PricePerLitrePaise);
    }

    [Fact]
    public async Task The_rate_in_force_defaults_to_today_in_the_household_time_zone()
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRateAsync(October1, 6000);
        await app.AddRateAsync(new DateOnly(2026, 10, 31), 6400); // starts tomorrow, in Asia/Kolkata

        Assert.Equal(6000, (await app.GetRateInEffect.HandleAsync(null, Ct)).PricePerLitrePaise);

        app.Clock.Set(new DateTimeOffset(2026, 10, 30, 18, 30, 0, TimeSpan.Zero)); // midnight arrives in Asia/Kolkata
        Assert.Equal(6400, (await app.GetRateInEffect.HandleAsync(null, Ct)).PricePerLitrePaise);
    }

    [Fact]
    public async Task Updating_changes_the_date_and_price()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRateAsync(October1, 6000);
        app.Database.ResetSaveCount();

        var updated = await app.UpdateRate.HandleAsync(new UpdateMilkRateCommand(added.Id, October15, 6400), Ct);

        Assert.Equal(new MilkRateDetails(added.Id, October15, 6400), updated);
        Assert.Equal(1, app.Database.SaveCount);
    }

    [Fact]
    public async Task A_rate_can_be_updated_without_changing_its_date()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRateAsync(October1, 6000);

        var updated = await app.UpdateRate.HandleAsync(new UpdateMilkRateCommand(added.Id, October1, 6100), Ct);

        Assert.Equal(6100, updated.PricePerLitrePaise);
    }

    [Fact]
    [Trait("Invariant", "T-3")]
    public async Task Moving_a_rate_onto_the_day_of_another_is_a_conflict_and_changes_nothing()
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRateAsync(October1, 6000);
        var other = await app.AddRateAsync(October15, 6400);
        app.Database.ResetSaveCount();

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => app.UpdateRate.HandleAsync(new UpdateMilkRateCommand(other.Id, October1, 7000), Ct));

        Assert.Equal(ErrorCodes.RateDateTaken, exception.Code);
        Assert.Equal(October15, app.Database.MilkRates.Single(rate => rate.Id.Value == other.Id).EffectiveFrom);
        Assert.Equal(0, app.Database.SaveCount);
    }

    [Fact]
    public async Task An_invalid_update_is_rejected_and_changes_nothing()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRateAsync(October1, 6000);
        app.Database.ResetSaveCount();

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => app.UpdateRate.HandleAsync(new UpdateMilkRateCommand(added.Id, October15, 0), Ct));

        Assert.Equal("pricePerLitrePaise", exception.Field);
        var stored = Assert.Single(app.Database.MilkRates);
        Assert.Equal(October1, stored.EffectiveFrom);
        Assert.Equal(6000, stored.Price.PaisePerLitre);
        Assert.Equal(0, app.Database.SaveCount);
    }

    [Fact]
    public async Task Updating_an_unknown_rate_is_not_found()
    {
        var app = await TestHousehold.SetUpAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => app.UpdateRate.HandleAsync(new UpdateMilkRateCommand(Guid.CreateVersion7(), October1, 6000), Ct));

        Assert.Equal(ErrorCodes.RateNotFound, exception.Code);
    }

    [Fact]
    public async Task Deleting_removes_the_rate()
    {
        var app = await TestHousehold.SetUpAsync();
        var keep = await app.AddRateAsync(October1, 6000);
        var remove = await app.AddRateAsync(October15, 6400);

        await app.DeleteRate.HandleAsync(remove.Id, Ct);

        Assert.Equal(keep.Id, Assert.Single(app.Database.MilkRates).Id.Value);
    }

    [Fact]
    public async Task Deleting_an_unknown_rate_is_not_found()
    {
        var app = await TestHousehold.SetUpAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => app.DeleteRate.HandleAsync(Guid.CreateVersion7(), Ct));

        Assert.Equal(ErrorCodes.RateNotFound, exception.Code);
    }
}
