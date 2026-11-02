using PureFreshMilk.Application.Rates;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.TestSupport;

public sealed class InMemoryMilkRateRepository(InMemoryDatabase database) : IMilkRateRepository
{
    public Task<IReadOnlyList<MilkRate>> ListAsync(HouseholdId householdId, CancellationToken cancellationToken)
    {
        IReadOnlyList<MilkRate> found = [.. database.MilkRates.Where(rate => rate.HouseholdId == householdId)];

        return Task.FromResult(found);
    }

    public Task<MilkRate?> FindAsync(HouseholdId householdId, MilkRateId id, CancellationToken cancellationToken) =>
        Task.FromResult(database.MilkRates.SingleOrDefault(rate => rate.HouseholdId == householdId && rate.Id == id));

    public Task<bool> EffectiveFromTakenAsync(
        HouseholdId householdId, DateOnly effectiveFrom, MilkRateId? exceptRate, CancellationToken cancellationToken) =>
        Task.FromResult(database.MilkRates.Any(rate =>
            rate.HouseholdId == householdId && rate.EffectiveFrom == effectiveFrom && rate.Id != exceptRate));

    public void Add(MilkRate rate) => database.StageChange(() => database.MilkRates.Add(rate));

    public void Remove(MilkRate rate) => database.StageChange(() => database.MilkRates.Remove(rate));
}
