using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Domain.Tests.Rates;

public sealed class PricePerLitreTests
{
    [Theory]
    [Trait("Invariant", "T-1")]
    [InlineData(1)]
    [InlineData(6250)]
    [InlineData(1_000_000)]
    public void A_price_from_1_to_1000000_paise_a_litre_is_accepted(long paise)
    {
        Assert.Equal(paise, PricePerLitre.FromPaise(paise).PaisePerLitre);
    }

    [Theory]
    [Trait("Invariant", "T-1")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    [InlineData(long.MaxValue)]
    public void Zero_negative_and_oversized_prices_are_rejected(long paise)
    {
        var exception = Assert.Throws<DomainException>(() => PricePerLitre.FromPaise(paise));

        Assert.Equal("pricePerLitrePaise", exception.Field);
    }

    [Theory]
    [Trait("Invariant", "B-3")]
    [InlineData(250, 6250, 1563)] // 1562.5 paise: the exact midpoint rounds up
    [InlineData(1000, 6250, 6250)]
    [InlineData(500, 3, 2)] // 1.5 paise: the exact midpoint rounds up
    [InlineData(499, 3, 1)] // 1.497 paise
    [InlineData(333, 3, 1)] // 0.999 paise
    [InlineData(1, 1, 0)] // 0.001 paise
    [InlineData(0, 6250, 0)]
    [InlineData(7500, 6250, 46_875)]
    public void The_cost_is_millilitres_times_price_over_1000_rounded_half_up(
        int millilitres, long pricePaise, long expectedPaise)
    {
        var cost = PricePerLitre.FromPaise(pricePaise).CostOf(Quantity.FromMillilitres(millilitres));

        Assert.Equal(expectedPaise, cost.Paise);
    }

    [Fact]
    [Trait("Invariant", "B-3")]
    public void Rounding_once_per_line_differs_from_rounding_every_delivery()
    {
        var price = PricePerLitre.FromPaise(6250);

        // 30 days of 250 ml: one line of 7,500 ml, as a calculator would give it.
        var once = price.CostOf(Quantity.FromMillilitres(30 * 250));
        // The rejected alternative: rounding every delivery overstates the bill by 15 paise.
        var perDelivery = Enumerable.Range(0, 30).Aggregate(
            Money.Zero, (sum, _) => sum + price.CostOf(Quantity.FromMillilitres(250)));

        Assert.Equal(Money.FromPaise(46_875), once);
        Assert.Equal(Money.FromPaise(46_890), perDelivery);
    }

    [Fact]
    [Trait("Invariant", "B-2")]
    public void A_rate_change_splits_a_month_into_one_line_per_rate()
    {
        // 1 L a day: ₹60.00 until 14 October (14 days), ₹64.00 from 15 October (17 days).
        var before = PricePerLitre.FromPaise(6000).CostOf(Quantity.FromMillilitres(14 * 1000));
        var after = PricePerLitre.FromPaise(6400).CostOf(Quantity.FromMillilitres(17 * 1000));

        Assert.Equal(Money.FromPaise(84_000), before);
        Assert.Equal(Money.FromPaise(108_800), after);
        Assert.Equal(Money.FromPaise(192_800), before + after);
    }

    [Fact]
    [Trait("Invariant", "B-4")]
    public void The_largest_quantity_at_the_largest_price_does_not_overflow()
    {
        var cost = PricePerLitre.FromPaise(PricePerLitre.MaxPaise).CostOf(Quantity.FromMillilitres(int.MaxValue));

        Assert.Equal(2_147_483_647_000L, cost.Paise);
    }

    [Fact]
    public void Prices_compare_by_amount()
    {
        Assert.True(PricePerLitre.FromPaise(6000).CompareTo(PricePerLitre.FromPaise(6400)) < 0);
        Assert.Equal(PricePerLitre.FromPaise(6000), PricePerLitre.FromPaise(6000));
    }

    [Fact]
    public void ToString_shows_paise_a_litre()
    {
        Assert.Equal("6250 paise a litre", PricePerLitre.FromPaise(6250).ToString());
    }
}
