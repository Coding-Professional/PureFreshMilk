namespace PureFreshMilk.Api.Features.Households;

/// <summary>Request body for <c>PUT /api/v1/household</c>.</summary>
/// <param name="TimeZoneId">An IANA id. Left out, it defaults to <c>Asia/Kolkata</c> on creation and is kept on update.</param>
internal sealed record SetUpHouseholdRequest(string? Name, string? TimeZoneId);
