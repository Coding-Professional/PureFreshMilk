using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Deliveries;

namespace PureFreshMilk.Application.Deliveries;

public sealed class ListDaySummariesHandler(ICurrentHousehold currentHousehold, IRecipientRepository recipients,
    IDeliveryRepository deliveries,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<DaySummary>> HandleAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        if (from == default || to == default || from > to || to.DayNumber - from.DayNumber >= 366)
        {
            throw new DomainException("Choose a date range of at most 366 days, from oldest to newest.", "from");
        }

        if (to > household.Today(timeProvider.GetUtcNow()))
        {
            throw new FutureDateException("to");
        }

        var recorded = await deliveries.ListAsync(household.Id, from, to, cancellationToken);
        var people = await recipients.ListAsync(household.Id, true, household.Today(timeProvider.GetUtcNow()), cancellationToken);
        var byDate = recorded.GroupBy(delivery => delivery.DeliveryDate)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var summaries = new List<DaySummary>();
        for (var date = to; date >= from; date = date.AddDays(-1))
        {
            var active = people.Where(person => person.IsActiveOn(date)).ToArray();
            var day = byDate.GetValueOrDefault(date) ?? [];
            summaries.Add(new DaySummary(date, new DayTotals(
                active.Sum(person => person.ExpectedQuantity.Millilitres),
                day.Sum(delivery => delivery.DeliveredQuantity.Millilitres),
                day.Count(delivery => delivery.Status == DeliveryStatus.Delivered),
                day.Count(delivery => delivery.Status == DeliveryStatus.Missed),
                active.Length - day.Length)));
            if (date == DateOnly.MinValue)
            {
                break;
            }
        }
        return summaries;
    }
}
