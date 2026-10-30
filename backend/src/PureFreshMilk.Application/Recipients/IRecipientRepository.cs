using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Application.Recipients;

/// <summary>Stores recipients. Implemented (internally) by Infrastructure; saved through <c>IUnitOfWork</c>.</summary>
public interface IRecipientRepository
{
    /// <summary>
    /// The household's recipients, ordered by name. Unless <paramref name="includeArchived"/> is set, a
    /// recipient already archived as of <paramref name="today"/> is left out.
    /// </summary>
    Task<IReadOnlyList<Recipient>> ListAsync(
        HouseholdId householdId, bool includeArchived, DateOnly today, CancellationToken cancellationToken);

    Task<Recipient?> FindAsync(HouseholdId householdId, RecipientId id, CancellationToken cancellationToken);

    /// <summary>
    /// Whether a recipient who is not archived already has this name, ignoring case. The database enforces the
    /// same rule with a partial unique index; this check gives a friendly answer first.
    /// </summary>
    Task<bool> NameInUseByActiveRecipientAsync(
        HouseholdId householdId, string name, RecipientId? exceptRecipient, CancellationToken cancellationToken);

    void Add(Recipient recipient);
}
