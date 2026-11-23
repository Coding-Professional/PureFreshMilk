using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Domain.Deliveries;

/// <summary>One recorded outcome for one recipient on one household calendar day.</summary>
public sealed class Delivery
{
    private Delivery() { }

    public DeliveryId Id { get; private set; }
    public HouseholdId HouseholdId { get; private set; }
    public RecipientId RecipientId { get; private set; }
    public DateOnly DeliveryDate { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public Quantity DeliveredQuantity { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Delivery Record(Recipient recipient, DateOnly date, DeliveryStatus status, int? deliveredMl, DateTimeOffset now)
    {
        if (!recipient.IsActiveOn(date))
        {
            throw new DomainException("The recipient is not active on this date.", "date");
        }

        var delivery = new Delivery
        {
            Id = DeliveryId.New(now),
            HouseholdId = recipient.HouseholdId,
            RecipientId = recipient.Id,
            DeliveryDate = date,
            CreatedAt = now,
        };
        delivery.Replace(status, deliveredMl, recipient.ExpectedQuantity, now);
        return delivery;
    }

    public void Replace(DeliveryStatus status, int? deliveredMl, Quantity usualQuantity, DateTimeOffset now)
    {
        var ml = status switch
        {
            DeliveryStatus.Delivered => deliveredMl ?? usualQuantity.Millilitres,
            DeliveryStatus.Missed => deliveredMl ?? 0,
            _ => throw new DomainException("The delivery status must be delivered or missed.", "status"),
        };
        if (status == DeliveryStatus.Delivered && ml is < 1 or > 20_000)
        {
            throw new DomainException("A delivered quantity must be between 1 and 20,000 ml.", "deliveredMl");
        }
        if (status == DeliveryStatus.Missed && ml != 0)
        {
            throw new DomainException("A missed delivery must have 0 ml.", "deliveredMl");
        }

        Status = status;
        DeliveredQuantity = Quantity.FromMillilitres(ml);
        UpdatedAt = now;
    }
}
