using PureFreshMilk.Application.Households;
using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.Infrastructure.Persistence.Repositories;

internal sealed class HouseholdRepository(PureFreshMilkDbContext context) : IHouseholdRepository
{
    public void Add(Household household) => context.Households.Add(household);
}
