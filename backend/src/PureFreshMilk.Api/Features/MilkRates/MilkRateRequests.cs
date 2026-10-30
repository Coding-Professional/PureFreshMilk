namespace PureFreshMilk.Api.Features.MilkRates;

/// <summary>Request body for <c>POST /api/v1/milk-rates</c> and <c>PUT /api/v1/milk-rates/{id}</c>.</summary>
/// <param name="EffectiveFrom">The first day the price applies. It may be in the past, to back-date a rise entered late.</param>
/// <param name="PricePerLitrePaise">The price of one litre, in whole paise.</param>
internal sealed record MilkRateRequest(DateOnly EffectiveFrom, long PricePerLitrePaise);
