using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.Application.Households;

/// <param name="TimeZoneId">An IANA id. Left out, it defaults to <c>Asia/Kolkata</c> on creation and is kept on update.</param>
public sealed record SetUpHouseholdCommand(string? Name, string? TimeZoneId);

/// <param name="Created"><c>true</c> if this call created the household, <c>false</c> if it updated the existing one.</param>
public sealed record SetUpHouseholdResult(HouseholdDetails Household, bool Created);

/// <summary>
/// Creates the household the first time and updates it afterwards. In single-household mode there is
/// never a second household.
/// </summary>
public sealed class SetUpHouseholdHandler(
    ICurrentHousehold currentHousehold,
    IHouseholdRepository households,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<SetUpHouseholdResult> HandleAsync(
        SetUpHouseholdCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var existing = await currentHousehold.FindAsync(cancellationToken);

        Household household;
        if (existing is null)
        {
            household = Household.Create(command.Name, command.TimeZoneId ?? Household.DefaultTimeZoneId, now);
            households.Add(household);
        }
        else
        {
            household = existing;
            household.Update(command.Name, command.TimeZoneId ?? household.TimeZoneId, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new SetUpHouseholdResult(HouseholdDetails.From(household, timeProvider), Created: existing is null);
    }
}
