using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Domain.Households;

/// <summary>Identifies a household. A distinct type so it cannot be passed where a recipient id is expected.</summary>
public readonly record struct HouseholdId
{
    private HouseholdId(Guid value) => Value = value;

    public Guid Value { get; }

    public static HouseholdId New(DateTimeOffset now) => new(Identifier.Create(now));

    public static HouseholdId From(Guid value) => new(Identifier.Require(value, "A household id"));

    public override string ToString() => Value.ToString();
}
