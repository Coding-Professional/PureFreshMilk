using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Application.Rates;

/// <param name="EffectiveFrom">The first day this price applies.</param>
/// <param name="PricePerLitrePaise">The price of one litre, in whole paise.</param>
public sealed record MilkRateDetails(Guid Id, DateOnly EffectiveFrom, long PricePerLitrePaise)
{
    public static MilkRateDetails From(MilkRate rate) =>
        new(rate.Id.Value, rate.EffectiveFrom, rate.Price.PaisePerLitre);
}
