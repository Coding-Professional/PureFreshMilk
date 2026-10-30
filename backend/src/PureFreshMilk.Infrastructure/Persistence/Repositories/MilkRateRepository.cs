using Microsoft.EntityFrameworkCore;
using PureFreshMilk.Application.Rates;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Infrastructure.Persistence.Repositories;

internal sealed class MilkRateRepository(PureFreshMilkDbContext context) : IMilkRateRepository
{
    public async Task<IReadOnlyList<MilkRate>> ListAsync(HouseholdId householdId, CancellationToken cancellationToken) =>
        await context.MilkRates
            .AsNoTracking()
            .Where(rate => rate.HouseholdId == householdId)
            .ToListAsync(cancellationToken);

    public async Task<MilkRate?> FindAsync(HouseholdId householdId, MilkRateId id, CancellationToken cancellationToken) =>
        await context.MilkRates.SingleOrDefaultAsync(
            rate => rate.HouseholdId == householdId && rate.Id == id, cancellationToken);

    public async Task<bool> EffectiveFromTakenAsync(
        HouseholdId householdId, DateOnly effectiveFrom, MilkRateId? exceptRate, CancellationToken cancellationToken) =>
        await context.MilkRates.AnyAsync(
            rate => rate.HouseholdId == householdId && rate.EffectiveFrom == effectiveFrom && rate.Id != exceptRate,
            cancellationToken);

    public void Add(MilkRate rate) => context.MilkRates.Add(rate);

    public void Remove(MilkRate rate) => context.MilkRates.Remove(rate);
}
