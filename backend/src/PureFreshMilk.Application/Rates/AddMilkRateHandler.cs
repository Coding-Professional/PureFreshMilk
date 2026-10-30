using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Application.Rates;

/// <param name="EffectiveFrom">The first day the price applies. It may be in the past, to back-date a rise entered late.</param>
/// <param name="PricePerLitrePaise">The price of one litre, in whole paise.</param>
public sealed record AddMilkRateCommand(DateOnly EffectiveFrom, long PricePerLitrePaise);

public sealed class AddMilkRateHandler(
    ICurrentHousehold currentHousehold,
    IMilkRateRepository rates,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<MilkRateDetails> HandleAsync(AddMilkRateCommand command, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);

        var rate = MilkRate.Create(
            household.Id,
            command.EffectiveFrom,
            PricePerLitre.FromPaise(command.PricePerLitrePaise),
            timeProvider.GetUtcNow());

        await MilkRateRules.EnsureDateIsFreeAsync(rates, household.Id, rate.EffectiveFrom, exceptRate: null, cancellationToken);

        rates.Add(rate);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MilkRateDetails.From(rate);
    }
}
