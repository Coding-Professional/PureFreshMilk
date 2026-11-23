using PureFreshMilk.Domain.Deliveries;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Application.Deliveries;

public interface IDeliveryRepository
{
    Task<Delivery?> FindAsync(HouseholdId householdId, RecipientId recipientId, DateOnly date, CancellationToken cancellationToken);
    Task<IReadOnlyList<Delivery>> ListAsync(HouseholdId householdId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
    Task<DateOnly?> FirstDateAsync(HouseholdId householdId, RecipientId recipientId, CancellationToken cancellationToken);
    Task<DateOnly?> LastDateAsync(HouseholdId householdId, RecipientId recipientId, CancellationToken cancellationToken);
    void Add(Delivery delivery);
    void Remove(Delivery delivery);
}
