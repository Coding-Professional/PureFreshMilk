using PureFreshMilk.Application.Households;

namespace PureFreshMilk.Api.Features.Households;

/// <summary>Response body for <c>GET</c> and <c>PUT /api/v1/household</c>.</summary>
/// <param name="TimeZoneId">An IANA time zone id, for example <c>Asia/Kolkata</c>.</param>
/// <param name="Today">The calendar date it is now in the household's time zone, decided by the server.</param>
internal sealed record HouseholdResponse(Guid Id, string Name, string TimeZoneId, DateOnly Today)
{
    public static HouseholdResponse From(HouseholdDetails household) =>
        new(household.Id, household.Name, household.TimeZoneId, household.Today);
}
