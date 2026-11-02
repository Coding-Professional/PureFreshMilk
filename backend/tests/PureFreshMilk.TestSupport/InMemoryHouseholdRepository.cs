using PureFreshMilk.Application.Households;
using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.TestSupport;

/// <summary>Stores households, and answers as the current household the way single-household mode does: the only one.</summary>
public sealed class InMemoryHouseholdRepository(InMemoryDatabase database) : IHouseholdRepository, ICurrentHousehold
{
    public void Add(Household household) => database.StageChange(() => database.Households.Add(household));

    public Task<Household?> FindAsync(CancellationToken cancellationToken) =>
        Task.FromResult(database.Households.SingleOrDefault());
}
