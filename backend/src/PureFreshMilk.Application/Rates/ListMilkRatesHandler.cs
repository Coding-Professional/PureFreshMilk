using PureFreshMilk.Application.Households;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Application.Rates;

/// <summary>Lists the household's rates, newest effective date first.</summary>
public sealed class ListMilkRatesHandler(ICurrentHousehold currentHousehold, IMilkRateRepository rates)
{
    public async Task<IReadOnlyList<MilkRateDetails>> HandleAsync(CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var schedule = new RateSchedule(await rates.ListAsync(household.Id, cancellationToken));

        return [.. schedule.Rates.Reverse().Select(MilkRateDetails.From)];
    }
}
