using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Domain.Deliveries;

namespace PureFreshMilk.Application.Deliveries;

public sealed class DeliverRemainingHandler(ICurrentHousehold currentHousehold, IRecipientRepository recipients,
    IDeliveryRepository deliveries, IUnitOfWork unitOfWork, GetDaySheetHandler sheets, TimeProvider timeProvider)
{
    public async Task<DaySheet> HandleAsync(string requestedDate, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var date = DeliveryRules.ResolveDate(requestedDate, household, now);
        var people = await recipients.ListAsync(household.Id, true, household.Today(now), cancellationToken);
        var recorded = await deliveries.ListAsync(household.Id, date, date, cancellationToken);
        var recordedIds = recorded.Select(delivery => delivery.RecipientId).ToHashSet();
        foreach (var person in people.Where(person => person.IsActiveOn(date) && !recordedIds.Contains(person.Id)))
        {
            deliveries.Add(Delivery.Record(person, date, DeliveryStatus.Delivered, null, now));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await sheets.HandleAsync(requestedDate, cancellationToken);
    }
}
