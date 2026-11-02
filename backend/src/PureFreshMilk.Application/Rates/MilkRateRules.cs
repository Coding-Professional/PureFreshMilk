using System.Globalization;
using PureFreshMilk.Application.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;

namespace PureFreshMilk.Application.Rates;

/// <summary>Checks shared by the milk rate use cases.</summary>
internal static class MilkRateRules
{
    public static async Task EnsureDateIsFreeAsync(
        IMilkRateRepository rates,
        HouseholdId householdId,
        DateOnly effectiveFrom,
        MilkRateId? exceptRate,
        CancellationToken cancellationToken)
    {
        if (await rates.EffectiveFromTakenAsync(householdId, effectiveFrom, exceptRate, cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.RateDateTaken,
                $"A price already starts on {effectiveFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}. Change that one instead.");
        }
    }

    public static async Task<MilkRate> RequireAsync(
        IMilkRateRepository rates, HouseholdId householdId, Guid id, CancellationToken cancellationToken)
    {
        var rate = id == Guid.Empty
            ? null
            : await rates.FindAsync(householdId, MilkRateId.From(id), cancellationToken);

        return rate ?? throw new NotFoundException(ErrorCodes.RateNotFound, "There is no such price.");
    }
}
