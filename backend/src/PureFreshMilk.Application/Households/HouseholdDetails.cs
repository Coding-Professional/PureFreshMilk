using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.Application.Households;

/// <param name="Today">The calendar date it is now in the household's time zone, decided by the server.</param>
public sealed record HouseholdDetails(Guid Id, string Name, string TimeZoneId, DateOnly Today)
{
    public static HouseholdDetails From(Household household, TimeProvider timeProvider) =>
        new(household.Id.Value, household.Name, household.TimeZoneId, household.Today(timeProvider.GetUtcNow()));
}
