using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Domain.Tests.Rates;

public sealed class RateScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 6, 0, 0, TimeSpan.Zero);
    private static readonly HouseholdId Household = HouseholdId.New(Now);

    private static MilkRate Rate(DateOnly from, long paise, HouseholdId? household = null) =>
        MilkRate.Create(household ?? Household, from, PricePerLitre.FromPaise(paise), Now);

    private static readonly MilkRate October = Rate(new DateOnly(2026, 10, 1), 6000);
    private static readonly MilkRate Mid = Rate(new DateOnly(2026, 10, 15), 6400);

    [Fact]
    [Trait("Invariant", "T-2")]
    public void There_is_no_rate_before_the_first_one()
    {
        var schedule = new RateSchedule([October, Mid]);

        Assert.Null(schedule.RateOn(new DateOnly(2026, 9, 30)));
    }

    [Fact]
    [Trait("Invariant", "T-2")]
    public void An_empty_schedule_has_no_rate_on_any_day()
    {
        Assert.Null(new RateSchedule([]).RateOn(new DateOnly(2026, 10, 30)));
    }

    [Theory]
    [Trait("Invariant", "T-2")]
    [InlineData("2026-10-01", 6000)] // the first rate applies on its own effective-from day
    [InlineData("2026-10-14", 6000)] // the old rate applies the day before the change
    [InlineData("2026-10-15", 6400)] // the new rate applies on its effective-from day
    [InlineData("2026-12-31", 6400)] // and keeps applying until a later rate starts
    public void The_rate_in_force_is_the_latest_one_starting_on_or_before_the_day(string date, long expectedPaise)
    {
        var schedule = new RateSchedule([October, Mid]);

        var rate = schedule.RateOn(DateOnly.Parse(date, System.Globalization.CultureInfo.InvariantCulture));

        Assert.NotNull(rate);
        Assert.Equal(expectedPaise, rate.Price.PaisePerLitre);
    }

    [Fact]
    [Trait("Invariant", "T-2")]
    public void The_order_rates_were_added_in_does_not_matter()
    {
        // A back-dated rate is added last but still governs the days it covers.
        var backDated = Rate(new DateOnly(2026, 9, 1), 5500);
        var inAddedOrder = new RateSchedule([October, Mid, backDated]);
        var reversed = new RateSchedule([backDated, Mid, October]);

        Assert.Equal(5500, inAddedOrder.RateOn(new DateOnly(2026, 9, 15))?.Price.PaisePerLitre);
        Assert.Equal(5500, reversed.RateOn(new DateOnly(2026, 9, 15))?.Price.PaisePerLitre);
        Assert.Equal(
            inAddedOrder.Rates.Select(rate => rate.EffectiveFrom),
            reversed.Rates.Select(rate => rate.EffectiveFrom));
        Assert.Equal(new DateOnly(2026, 9, 1), inAddedOrder.Rates[0].EffectiveFrom);
    }

    [Fact]
    public void Two_rates_cannot_start_on_the_same_day()
    {
        var duplicate = Rate(new DateOnly(2026, 10, 1), 7000);

        var exception = Assert.Throws<DomainException>(() => new RateSchedule([October, duplicate]));

        Assert.Equal("effectiveFrom", exception.Field);
    }

    [Fact]
    public void A_schedule_cannot_mix_households()
    {
        var other = Rate(new DateOnly(2026, 11, 1), 6500, HouseholdId.New(Now));

        Assert.Throws<DomainException>(() => new RateSchedule([October, other]));
    }
}
