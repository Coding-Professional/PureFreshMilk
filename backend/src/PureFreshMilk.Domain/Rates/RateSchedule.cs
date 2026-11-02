using System.Globalization;
using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Domain.Rates;

/// <summary>
/// One household's rates, ordered by effective-from date. Answers "which price applies on this day"
/// without a database, so the rule is unit-testable.
/// </summary>
public sealed class RateSchedule
{
    private readonly MilkRate[] rates;

    public RateSchedule(IEnumerable<MilkRate> rates)
    {
        this.rates = [.. rates.OrderBy(rate => rate.EffectiveFrom)];

        for (var index = 1; index < this.rates.Length; index++)
        {
            if (this.rates[index].EffectiveFrom == this.rates[index - 1].EffectiveFrom)
            {
                throw new DomainException(
                    $"Two rates cannot start on the same date ({this.rates[index].EffectiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}).",
                    "effectiveFrom");
            }

            if (this.rates[index].HouseholdId != this.rates[0].HouseholdId)
            {
                throw new DomainException("A rate schedule belongs to a single household.");
            }
        }
    }

    /// <summary>The rates, earliest first.</summary>
    public IReadOnlyList<MilkRate> Rates => rates;

    /// <summary>
    /// The rate in force on <paramref name="date"/>: the one with the latest effective-from date on or before it,
    /// or <c>null</c> before the first rate (such a day has no price; it is never priced at zero).
    /// </summary>
    public MilkRate? RateOn(DateOnly date)
    {
        MilkRate? inForce = null;

        foreach (var rate in rates)
        {
            if (rate.EffectiveFrom > date)
            {
                break;
            }

            inForce = rate;
        }

        return inForce;
    }
}
