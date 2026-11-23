using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Domain.Deliveries;

public readonly record struct DeliveryId
{
    private DeliveryId(Guid value) => Value = value;
    public Guid Value { get; }
    public static DeliveryId New(DateTimeOffset now) => new(Identifier.Create(now));
    public static DeliveryId From(Guid value) => new(Identifier.Require(value, "A delivery id"));
}
