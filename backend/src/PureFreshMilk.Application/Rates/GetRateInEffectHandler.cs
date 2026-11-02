using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Application.Rates;

/// <summary>The price in force on a date. A day before the first rate has no price, which is not the same as free.</summary>
public sealed class GetRateInEffectHandler(
    ICurrentHousehold currentHousehold, IMilkRateRepository rates, TimeProvider timeProvider)
{
    /// <param name="on">The date to price. Left out, it is today in the household's time zone.</param>
    public async Task<MilkRateDetails> HandleAsync(DateOnly? on, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var schedule = new RateSchedule(await rates.ListAsync(household.Id, cancellationToken));

        var rate = schedule.RateOn(on ?? household.Today(timeProvider.GetUtcNow()))
            ?? throw new NotFoundException(ErrorCodes.NoRateInEffect, "No price is in force on that date.");

        return MilkRateDetails.From(rate);
    }
}
