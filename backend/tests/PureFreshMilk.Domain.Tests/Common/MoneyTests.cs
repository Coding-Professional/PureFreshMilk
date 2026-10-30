using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Domain.Tests.Common;

public sealed class MoneyTests
{
    [Fact]
    public void Default_money_is_zero()
    {
        Assert.Equal(0, default(Money).Paise);
        Assert.Equal(Money.Zero, default);
    }

    [Fact]
    public void Money_adds_and_subtracts_exactly()
    {
        var total = Money.FromPaise(46_875) + Money.FromPaise(15) - Money.FromPaise(90);

        Assert.Equal(Money.FromPaise(46_800), total);
    }

    [Fact]
    public void Money_can_be_negative_because_adjustments_can_be()
    {
        Assert.Equal(-250, Money.FromPaise(-250).Paise);
        Assert.Equal(Money.FromPaise(-250), -Money.FromPaise(250));
        Assert.Equal(Money.FromPaise(-100), Money.FromPaise(50) - Money.FromPaise(150));
    }

    [Fact]
    [Trait("Invariant", "B-4")]
    public void Arithmetic_that_overflows_throws()
    {
        var largest = Money.FromPaise(long.MaxValue);
        var smallest = Money.FromPaise(long.MinValue);

        Assert.Throws<OverflowException>(() => largest + Money.FromPaise(1));
        Assert.Throws<OverflowException>(() => smallest - Money.FromPaise(1));
        Assert.Throws<OverflowException>(() => -smallest);
    }

    [Fact]
    public void Money_compares_by_amount()
    {
        var small = Money.FromPaise(100);
        var large = Money.FromPaise(200);

        Assert.True(small < large);
        Assert.True(large > small);
        Assert.True(small <= Money.FromPaise(100));
        Assert.True(large >= small);
    }

    [Fact]
    public void ToString_shows_paise()
    {
        Assert.Equal("6250 paise", Money.FromPaise(6250).ToString());
    }
}
