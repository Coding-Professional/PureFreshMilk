using PureFreshMilk.Application.Common;
using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.Application.Households;

/// <summary>
/// The household the caller is working in. Until authentication exists, Infrastructure answers with the
/// only household (single-household mode); later it will come from the signed-in user, and no use case
/// changes (ADR 0004).
/// </summary>
public interface ICurrentHousehold
{
    /// <summary>The household, or <c>null</c> if none has been set up yet.</summary>
    Task<Household?> FindAsync(CancellationToken cancellationToken);
}

public static class CurrentHouseholdExtensions
{
    /// <summary>The household, or <see cref="NotFoundException"/> if none has been set up yet.</summary>
    public static async Task<Household> RequireAsync(
        this ICurrentHousehold currentHousehold, CancellationToken cancellationToken) =>
        await currentHousehold.FindAsync(cancellationToken)
        ?? throw new NotFoundException(
            ErrorCodes.HouseholdNotSetUp, "The household has not been set up yet.");
}
