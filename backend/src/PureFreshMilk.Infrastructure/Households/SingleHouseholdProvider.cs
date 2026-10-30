using Microsoft.EntityFrameworkCore;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Infrastructure.Persistence;

namespace PureFreshMilk.Infrastructure.Households;

/// <summary>
/// Single-household mode: before authentication exists, the current household is the only one in the
/// database. It will be replaced by an implementation that reads the household from the signed-in user (ADR 0004).
/// </summary>
internal sealed class SingleHouseholdProvider(PureFreshMilkDbContext context) : ICurrentHousehold
{
    private Household? household;

    public async Task<Household?> FindAsync(CancellationToken cancellationToken)
    {
        if (household is not null)
        {
            return household;
        }

        // Ask for two so that a second household is noticed rather than silently ignored.
        var found = await context.Households.OrderBy(candidate => candidate.Id).Take(2).ToListAsync(cancellationToken);

        if (found.Count > 1)
        {
            throw new InvalidOperationException(
                "Single-household mode found more than one household. Remove the extra rows or enable authentication.");
        }

        return household = found.SingleOrDefault();
    }
}
