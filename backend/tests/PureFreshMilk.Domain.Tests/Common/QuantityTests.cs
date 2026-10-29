using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Domain.Tests.Common;

public sealed class QuantityTests
{
    [Fact]
    public void Default_quantity_is_zero()
    {
        Assert.Equal(0, default(Quantity).Millilitres);
        Assert.Equal(Quantity.Zero, default);
    }

    [Fact]
    public void FromMillilitres_rejects_negative_values()
    {
        Assert.Throws<DomainException>(() => Quantity.FromMillilitres(-1));
    }

    [Theory]
    [InlineData("1", 1000)]
    [InlineData("0.5", 500)]
    [InlineData("1.25", 1250)]
    [InlineData("0", 0)]
    public void FromLitres_converts_to_whole_millilitres(string litres, int expectedMillilitres)
    {
        var quantity = Quantity.FromLitres(decimal.Parse(litres, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(expectedMillilitres, quantity.Millilitres);
    }

    [Fact]
    public void FromLitres_rejects_fractions_of_a_millilitre()
    {
        Assert.Throws<DomainException>(() => Quantity.FromLitres(0.0005m));
    }

    [Fact]
    public void FromLitres_rejects_negative_values()
    {
        Assert.Throws<DomainException>(() => Quantity.FromLitres(-0.5m));
    }

    [Fact]
    public void Quantities_add_exactly()
    {
        // The example household: 1 L + 500 ml + 500 ml per day.
        Quantity[] recipients =
        [
            Quantity.FromLitres(1m),
            Quantity.FromMillilitres(500),
            Quantity.FromMillilitres(500),
        ];

        var total = recipients.Aggregate(Quantity.Zero, (sum, next) => sum + next);

        Assert.Equal(Quantity.FromLitres(2m), total);
    }

    [Fact]
    public void Addition_that_overflows_throws()
    {
        var huge = Quantity.FromMillilitres(int.MaxValue);

        Assert.Throws<OverflowException>(() => huge + Quantity.FromMillilitres(1));
    }

    [Fact]
    public void Quantities_compare_by_volume()
    {
        var half = Quantity.FromMillilitres(500);
        var litre = Quantity.FromLitres(1m);

        Assert.True(half < litre);
        Assert.True(litre > half);
        Assert.True(half <= Quantity.FromMillilitres(500));
        Assert.True(litre >= half);
        Assert.Equal(Quantity.FromMillilitres(1000), litre);
    }

    [Fact]
    public void ToString_shows_millilitres()
    {
        Assert.Equal("1500 ml", Quantity.FromMillilitres(1500).ToString());
    }
}
