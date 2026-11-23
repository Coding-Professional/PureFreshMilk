using PureFreshMilk.Application.Deliveries;
using PureFreshMilk.Domain.Deliveries;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.TestSupport;

public sealed class InMemoryDeliveryRepository(InMemoryDatabase database) : IDeliveryRepository
{
    public Task<Delivery?> FindAsync(HouseholdId householdId, RecipientId recipientId, DateOnly date,
        CancellationToken cancellationToken) => Task.FromResult(database.Deliveries.SingleOrDefault(delivery =>
            delivery.HouseholdId == householdId && delivery.RecipientId == recipientId && delivery.DeliveryDate == date));

    public Task<IReadOnlyList<Delivery>> ListAsync(HouseholdId householdId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Delivery>>(database.Deliveries.Where(delivery =>
            delivery.HouseholdId == householdId && delivery.DeliveryDate >= from && delivery.DeliveryDate <= to).ToArray());

    public Task<DateOnly?> FirstDateAsync(HouseholdId householdId, RecipientId recipientId,
        CancellationToken cancellationToken) => Task.FromResult(database.Deliveries.Where(delivery =>
            delivery.HouseholdId == householdId && delivery.RecipientId == recipientId)
            .Select(delivery => (DateOnly?)delivery.DeliveryDate).Min());

    public Task<DateOnly?> LastDateAsync(HouseholdId householdId, RecipientId recipientId,
        CancellationToken cancellationToken) => Task.FromResult(database.Deliveries.Where(delivery =>
            delivery.HouseholdId == householdId && delivery.RecipientId == recipientId)
            .Select(delivery => (DateOnly?)delivery.DeliveryDate).Max());

    public void Add(Delivery delivery) => database.StageChange(() => database.Deliveries.Add(delivery));
    public void Remove(Delivery delivery) => database.StageChange(() => database.Deliveries.Remove(delivery));
}
