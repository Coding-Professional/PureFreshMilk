using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Application.Rates;

/// <summary>
/// Changes the date or price of a rate. Once billing exists, a rate that governs a finalised month will be
/// refused here (<c>period-finalised</c>).
/// </summary>
public sealed record UpdateMilkRateCommand(Guid Id, DateOnly EffectiveFrom, long PricePerLitrePaise);

public sealed class UpdateMilkRateHandler(
    ICurrentHousehold currentHousehold,
    IMilkRateRepository rates,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<MilkRateDetails> HandleAsync(UpdateMilkRateCommand command, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var rate = await MilkRateRules.RequireAsync(rates, household.Id, command.Id, cancellationToken);

        // Check the price and look for a clash before changing anything, so a rejected request leaves the rate as it was.
        var price = PricePerLitre.FromPaise(command.PricePerLitrePaise);
        await MilkRateRules.EnsureDateIsFreeAsync(rates, household.Id, command.EffectiveFrom, rate.Id, cancellationToken);

        rate.Change(command.EffectiveFrom, price, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MilkRateDetails.From(rate);
    }
}
