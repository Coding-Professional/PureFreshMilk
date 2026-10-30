using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Domain.Tests.Rates;

public sealed class MilkRateTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 6, 0, 0, TimeSpan.Zero);
    private static readonly HouseholdId Household = HouseholdId.New(Now);

    [Fact]
    public void Create_stores_the_details()
    {
        var rate = MilkRate.Create(Household, new DateOnly(2026, 10, 1), PricePerLitre.FromPaise(6250), Now);

        Assert.NotEqual(Guid.Empty, rate.Id.Value);
        Assert.Equal(Household, rate.HouseholdId);
        Assert.Equal(new DateOnly(2026, 10, 1), rate.EffectiveFrom);
        Assert.Equal(6250, rate.Price.PaisePerLitre);
        Assert.Equal(Now, rate.CreatedAt);
        Assert.Equal(Now, rate.UpdatedAt);
    }

    [Fact]
    [Trait("Invariant", "T-1")]
    public void The_effective_from_date_is_required()
    {
        var exception = Assert.Throws<DomainException>(
            () => MilkRate.Create(Household, default, PricePerLitre.FromPaise(6250), Now));

        Assert.Equal("effectiveFrom", exception.Field);
    }

    [Fact]
    [Trait("Invariant", "T-1")]
    public void A_default_price_of_zero_is_rejected()
    {
        var exception = Assert.Throws<DomainException>(
            () => MilkRate.Create(Household, new DateOnly(2026, 10, 1), default, Now));

        Assert.Equal("pricePerLitrePaise", exception.Field);
    }

    [Fact]
    [Trait("Invariant", "T-4")]
    public void A_rate_can_start_in_the_past()
    {
        var rate = MilkRate.Create(Household, new DateOnly(2020, 1, 1), PricePerLitre.FromPaise(4000), Now);

        Assert.Equal(new DateOnly(2020, 1, 1), rate.EffectiveFrom);
    }

    [Fact]
    public void Change_updates_the_date_price_and_timestamp()
    {
        var rate = MilkRate.Create(Household, new DateOnly(2026, 10, 1), PricePerLitre.FromPaise(6250), Now);
        var later = Now.AddHours(1);

        rate.Change(new DateOnly(2026, 10, 5), PricePerLitre.FromPaise(6400), later);

        Assert.Equal(new DateOnly(2026, 10, 5), rate.EffectiveFrom);
        Assert.Equal(6400, rate.Price.PaisePerLitre);
        Assert.Equal(Now, rate.CreatedAt);
        Assert.Equal(later, rate.UpdatedAt);
    }

    [Fact]
    public void A_rejected_change_leaves_the_rate_unchanged()
    {
        var rate = MilkRate.Create(Household, new DateOnly(2026, 10, 1), PricePerLitre.FromPaise(6250), Now);

        Assert.Throws<DomainException>(() => rate.Change(default, PricePerLitre.FromPaise(6400), Now.AddHours(1)));

        Assert.Equal(new DateOnly(2026, 10, 1), rate.EffectiveFrom);
        Assert.Equal(6250, rate.Price.PaisePerLitre);
        Assert.Equal(Now, rate.UpdatedAt);
    }
}
