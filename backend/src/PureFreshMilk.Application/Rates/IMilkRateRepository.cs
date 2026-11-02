using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Application.Rates;

/// <summary>Stores milk rates. Implemented (internally) by Infrastructure; saved through <c>IUnitOfWork</c>.</summary>
public interface IMilkRateRepository
{
    /// <summary>Every rate of the household, in no particular order.</summary>
    Task<IReadOnlyList<MilkRate>> ListAsync(HouseholdId householdId, CancellationToken cancellationToken);

    Task<MilkRate?> FindAsync(HouseholdId householdId, MilkRateId id, CancellationToken cancellationToken);

    /// <summary>
    /// Whether the household already has a rate starting on <paramref name="effectiveFrom"/>. The database enforces
    /// the same rule with a unique index; this check gives a friendly answer first.
    /// </summary>
    Task<bool> EffectiveFromTakenAsync(
        HouseholdId householdId, DateOnly effectiveFrom, MilkRateId? exceptRate, CancellationToken cancellationToken);

    void Add(MilkRate rate);

    void Remove(MilkRate rate);
}
