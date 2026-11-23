using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Domain.Deliveries;

namespace PureFreshMilk.Application.Deliveries;

public sealed class GetDaySheetHandler(ICurrentHousehold currentHousehold, IRecipientRepository recipients,
    IDeliveryRepository deliveries, TimeProvider timeProvider)
{
    public async Task<DaySheet> HandleAsync(string requestedDate, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var date = DeliveryRules.ResolveDate(requestedDate, household, now);
        var people = await recipients.ListAsync(household.Id, true, household.Today(now), cancellationToken);
        var recorded = await deliveries.ListAsync(household.Id, date, date, cancellationToken);
        var byRecipient = recorded.ToDictionary(delivery => delivery.RecipientId);
        var rows = people.Where(person => person.IsActiveOn(date)).Select(person =>
        {
            byRecipient.TryGetValue(person.Id, out var delivery);
            return new DayRow(person.Id.Value, person.Name, person.ExpectedQuantity.Millilitres,
                delivery?.Status switch
                {
                    DeliveryStatus.Delivered => "delivered",
                    DeliveryStatus.Missed => "missed",
                    _ => "notRecorded",
                }, delivery?.DeliveredQuantity.Millilitres ?? 0);
        }).ToArray();
        return new DaySheet(date, date == household.Today(now), rows, new DayTotals(
            rows.Sum(row => row.ExpectedMl), rows.Sum(row => row.DeliveredMl),
            rows.Count(row => row.Status == "delivered"), rows.Count(row => row.Status == "missed"),
            rows.Count(row => row.Status == "notRecorded")));
    }
}
