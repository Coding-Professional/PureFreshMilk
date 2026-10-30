using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.TestSupport;

/// <summary>
/// A stand-in for the database. Like a real unit of work, it keeps what a use case adds or removes
/// to itself until <see cref="SaveChangesAsync"/> is called, so a handler that forgets to save fails its test.
/// It hands out the stored entity instances, so a handler must check a request before changing an
/// entity (as the real handlers do): nothing here can roll a change back.
/// </summary>
public sealed class InMemoryDatabase : IUnitOfWork
{
    private readonly List<Action> pendingChanges = [];

    public List<Household> Households { get; } = [];

    public List<Recipient> Recipients { get; } = [];

    public List<MilkRate> MilkRates { get; } = [];

    /// <summary>How many times <see cref="SaveChangesAsync"/> has been called.</summary>
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var change in pendingChanges)
        {
            change();
        }

        pendingChanges.Clear();
        SaveCount++;
        return Task.CompletedTask;
    }

    /// <summary>Forgets earlier saves, so a test can check that the step under test saved (or did not save).</summary>
    public void ResetSaveCount() => SaveCount = 0;

    /// <summary>Empties the database, so tests that share a host start from nothing.</summary>
    public void Clear()
    {
        Households.Clear();
        Recipients.Clear();
        MilkRates.Clear();
        pendingChanges.Clear();
        SaveCount = 0;
    }

    internal void StageChange(Action change) => pendingChanges.Add(change);
}
