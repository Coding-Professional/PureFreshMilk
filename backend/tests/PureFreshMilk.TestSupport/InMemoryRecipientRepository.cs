using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.TestSupport;

public sealed class InMemoryRecipientRepository(InMemoryDatabase database) : IRecipientRepository
{
    public Task<IReadOnlyList<Recipient>> ListAsync(
        HouseholdId householdId, bool includeArchived, DateOnly today, CancellationToken cancellationToken)
    {
        IReadOnlyList<Recipient> found =
        [
            .. database.Recipients
                .Where(recipient => recipient.HouseholdId == householdId)
                .Where(recipient => includeArchived || !recipient.IsArchivedOn(today))
                .OrderBy(recipient => recipient.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(recipient => recipient.Id.Value),
        ];

        return Task.FromResult(found);
    }

    public Task<Recipient?> FindAsync(HouseholdId householdId, RecipientId id, CancellationToken cancellationToken) =>
        Task.FromResult(database.Recipients.SingleOrDefault(
            recipient => recipient.HouseholdId == householdId && recipient.Id == id));

    public Task<bool> NameInUseByActiveRecipientAsync(
        HouseholdId householdId, string name, RecipientId? exceptRecipient, CancellationToken cancellationToken) =>
        Task.FromResult(database.Recipients.Any(recipient =>
            recipient.HouseholdId == householdId
            && recipient.ArchivedFrom is null
            && recipient.Id != exceptRecipient
            && string.Equals(recipient.Name, name, StringComparison.OrdinalIgnoreCase)));

    public void Add(Recipient recipient) => database.StageChange(() => database.Recipients.Add(recipient));
}
