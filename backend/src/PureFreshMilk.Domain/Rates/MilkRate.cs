using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.Domain.Rates;

/// <summary>
/// The price of a litre of milk for a household from a given day. A delivery is priced at the rate with
/// the latest effective-from date on or before the delivery date (see <see cref="RateSchedule"/>).
/// </summary>
public sealed class MilkRate
{
    // Persistence constructor.
    private MilkRate()
    {
    }

    public MilkRateId Id { get; private set; }

    public HouseholdId HouseholdId { get; private set; }

    /// <summary>The first day this price applies.</summary>
    public DateOnly EffectiveFrom { get; private set; }

    public PricePerLitre Price { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static MilkRate Create(
        HouseholdId householdId, DateOnly effectiveFrom, PricePerLitre price, DateTimeOffset now)
    {
        var rate = new MilkRate
        {
            Id = MilkRateId.New(now),
            HouseholdId = householdId,
            CreatedAt = now,
        };
        rate.Apply(effectiveFrom, price, now);
        return rate;
    }

    public void Change(DateOnly effectiveFrom, PricePerLitre price, DateTimeOffset now) =>
        Apply(effectiveFrom, price, now);

    private void Apply(DateOnly effectiveFrom, PricePerLitre price, DateTimeOffset now)
    {
        if (effectiveFrom == default)
        {
            throw new DomainException("The date the price starts on is required.", "effectiveFrom");
        }

        // A default PricePerLitre (0 paise) bypasses FromPaise, so check the value again.
        _ = PricePerLitre.FromPaise(price.PaisePerLitre);

        EffectiveFrom = effectiveFrom;
        Price = price;
        UpdatedAt = now;
    }
}
