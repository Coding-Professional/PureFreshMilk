using Microsoft.EntityFrameworkCore;
using PureFreshMilk.Application.Deliveries;
using PureFreshMilk.Domain.Deliveries;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Infrastructure.Persistence.Repositories;

internal sealed class DeliveryRepository(PureFreshMilkDbContext context) : IDeliveryRepository
{
    public Task<Delivery?> FindAsync(HouseholdId householdId, RecipientId recipientId, DateOnly date,
        CancellationToken cancellationToken) => context.Deliveries.SingleOrDefaultAsync(delivery =>
            delivery.HouseholdId == householdId && delivery.RecipientId == recipientId && delivery.DeliveryDate == date,
            cancellationToken);

    public async Task<IReadOnlyList<Delivery>> ListAsync(HouseholdId householdId, DateOnly from, DateOnly to,
        CancellationToken cancellationToken) => await context.Deliveries.AsNoTracking().Where(delivery =>
            delivery.HouseholdId == householdId && delivery.DeliveryDate >= from && delivery.DeliveryDate <= to)
            .ToListAsync(cancellationToken);

    public Task<DateOnly?> FirstDateAsync(HouseholdId householdId, RecipientId recipientId,
        CancellationToken cancellationToken) => context.Deliveries.Where(delivery =>
            delivery.HouseholdId == householdId && delivery.RecipientId == recipientId)
            .MinAsync(delivery => (DateOnly?)delivery.DeliveryDate, cancellationToken);

    public Task<DateOnly?> LastDateAsync(HouseholdId householdId, RecipientId recipientId,
        CancellationToken cancellationToken) => context.Deliveries.Where(delivery =>
            delivery.HouseholdId == householdId && delivery.RecipientId == recipientId)
            .MaxAsync(delivery => (DateOnly?)delivery.DeliveryDate, cancellationToken);

    public void Add(Delivery delivery) => context.Deliveries.Add(delivery);
    public void Remove(Delivery delivery) => context.Deliveries.Remove(delivery);
}
