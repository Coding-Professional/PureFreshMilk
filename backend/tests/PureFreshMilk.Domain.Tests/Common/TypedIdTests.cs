using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Domain.Tests.Common;

public sealed class TypedIdTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_ids_are_non_empty_version_7_uuids()
    {
        Guid[] values = [HouseholdId.New(Now).Value, RecipientId.New(Now).Value, MilkRateId.New(Now).Value];

        Assert.All(values, value =>
        {
            Assert.NotEqual(Guid.Empty, value);
            Assert.Equal(7, value.Version);
        });
    }

    [Fact]
    public void New_ids_are_unique()
    {
        Assert.NotEqual(RecipientId.New(Now), RecipientId.New(Now));
    }

    [Fact]
    public void From_round_trips_a_value()
    {
        var value = Guid.CreateVersion7(Now);

        Assert.Equal(value, HouseholdId.From(value).Value);
        Assert.Equal(value, RecipientId.From(value).Value);
        Assert.Equal(value, MilkRateId.From(value).Value);
    }

    [Fact]
    public void From_rejects_an_empty_guid()
    {
        Assert.Throws<DomainException>(() => HouseholdId.From(Guid.Empty));
        Assert.Throws<DomainException>(() => RecipientId.From(Guid.Empty));
        Assert.Throws<DomainException>(() => MilkRateId.From(Guid.Empty));
    }

    [Fact]
    public void ToString_is_the_guid()
    {
        var value = Guid.CreateVersion7(Now);

        Assert.Equal(value.ToString(), RecipientId.From(value).ToString());
    }
}
