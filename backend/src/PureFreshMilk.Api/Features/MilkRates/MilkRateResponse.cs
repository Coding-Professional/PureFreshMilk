using PureFreshMilk.Application.Rates;

namespace PureFreshMilk.Api.Features.MilkRates;

/// <summary>A milk rate in API responses.</summary>
/// <param name="EffectiveFrom">The first day this price applies (<c>YYYY-MM-DD</c>).</param>
/// <param name="PricePerLitrePaise">The price of one litre, in whole paise.</param>
internal sealed record MilkRateResponse(Guid Id, DateOnly EffectiveFrom, long PricePerLitrePaise)
{
    public static MilkRateResponse From(MilkRateDetails rate) =>
        new(rate.Id, rate.EffectiveFrom, rate.PricePerLitrePaise);
}
