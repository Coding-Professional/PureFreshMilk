using Microsoft.EntityFrameworkCore;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;
using PureFreshMilk.Infrastructure.Persistence.Configurations;

namespace PureFreshMilk.Infrastructure.Persistence.Repositories;

internal sealed class RecipientRepository(PureFreshMilkDbContext context) : IRecipientRepository
{
    public async Task<IReadOnlyList<Recipient>> ListAsync(
        HouseholdId householdId, bool includeArchived, DateOnly today, CancellationToken cancellationToken) =>
        await context.Recipients
            .AsNoTracking()
            .Where(recipient => recipient.HouseholdId == householdId)
            .Where(recipient => includeArchived || recipient.ArchivedFrom == null || recipient.ArchivedFrom > today)
            .OrderBy(recipient => EF.Property<string>(recipient, RecipientConfiguration.NameKey))
            .ThenBy(recipient => recipient.Id)
            .ToListAsync(cancellationToken);

    public async Task<Recipient?> FindAsync(
        HouseholdId householdId, RecipientId id, CancellationToken cancellationToken) =>
        await context.Recipients.SingleOrDefaultAsync(
            recipient => recipient.HouseholdId == householdId && recipient.Id == id, cancellationToken);

    public async Task<bool> NameInUseByActiveRecipientAsync(
        HouseholdId householdId, string name, RecipientId? exceptRecipient, CancellationToken cancellationToken)
    {
        // Compare the generated lower-case column, the same one the unique index is built on.
        var nameKey = name.ToLowerInvariant();

        return await context.Recipients.AnyAsync(
            recipient => recipient.HouseholdId == householdId
                && recipient.ArchivedFrom == null
                && recipient.Id != exceptRecipient
                && EF.Property<string>(recipient, RecipientConfiguration.NameKey) == nameKey,
            cancellationToken);
    }

    public void Add(Recipient recipient) => context.Recipients.Add(recipient);
}
