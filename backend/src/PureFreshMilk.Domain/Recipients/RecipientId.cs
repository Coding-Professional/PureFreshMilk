using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Domain.Recipients;

/// <summary>Identifies a recipient. A distinct type so it cannot be passed where a household id is expected.</summary>
public readonly record struct RecipientId
{
    private RecipientId(Guid value) => Value = value;

    public Guid Value { get; }

    public static RecipientId New(DateTimeOffset now) => new(Identifier.Create(now));

    public static RecipientId From(Guid value) => new(Identifier.Require(value, "A recipient id"));

    public override string ToString() => Value.ToString();
}
