using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Domain.Rates;

/// <summary>Identifies a milk rate.</summary>
public readonly record struct MilkRateId
{
    private MilkRateId(Guid value) => Value = value;

    public Guid Value { get; }

    public static MilkRateId New(DateTimeOffset now) => new(Identifier.Create(now));

    public static MilkRateId From(Guid value) => new(Identifier.Require(value, "A milk rate id"));

    public override string ToString() => Value.ToString();
}
