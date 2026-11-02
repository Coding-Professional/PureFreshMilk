using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.Application.Households;

/// <summary>Stores households. Implemented (internally) by Infrastructure; saved through <c>IUnitOfWork</c>.</summary>
public interface IHouseholdRepository
{
    void Add(Household household);
}
