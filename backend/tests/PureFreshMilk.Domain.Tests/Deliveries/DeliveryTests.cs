using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Deliveries;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Domain.Tests.Deliveries;

public sealed class DeliveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 6, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Date = new(2026, 10, 30);

    private static Recipient Person() => Recipient.Add(HouseholdId.New(Now), "Recipient A",
        Quantity.FromMillilitres(750), new DateOnly(2026, 10, 1), Now);

    [Fact]
    [Trait("Invariant", "L-1,L-2,L-3,L-4,L-7")]
    public void Recording_and_replacing_enforce_status_and_quantity()
    {
        var person = Person();
        var delivery = Delivery.Record(person, Date, DeliveryStatus.Delivered, null, Now);
        Assert.Equal(750, delivery.DeliveredQuantity.Millilitres);

        delivery.Replace(DeliveryStatus.Missed, null, person.ExpectedQuantity, Now.AddMinutes(1));
        Assert.Equal(0, delivery.DeliveredQuantity.Millilitres);
        delivery.Replace(DeliveryStatus.Delivered, null, person.ExpectedQuantity, Now.AddMinutes(2));
        Assert.Equal(750, delivery.DeliveredQuantity.Millilitres);

        Assert.Throws<DomainException>(() => delivery.Replace(DeliveryStatus.Delivered, 0, person.ExpectedQuantity, Now));
        Assert.Throws<DomainException>(() => delivery.Replace(DeliveryStatus.Delivered, 20_001, person.ExpectedQuantity, Now));
        Assert.Throws<DomainException>(() => delivery.Replace(DeliveryStatus.Missed, 500, person.ExpectedQuantity, Now));
        Assert.Equal(750, delivery.DeliveredQuantity.Millilitres);
    }

    [Fact]
    [Trait("Invariant", "L-6")]
    public void Inactive_recipient_cannot_receive_a_delivery()
    {
        Assert.Throws<DomainException>(() => Delivery.Record(Person(), new DateOnly(2026, 9, 30),
            DeliveryStatus.Delivered, null, Now));
    }
}
