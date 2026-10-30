namespace PureFreshMilk.Api.Features.Recipients;

/// <summary>Request body for <c>POST /api/v1/recipients</c>.</summary>
/// <param name="ExpectedMl">The usual daily quantity, in whole millilitres.</param>
/// <param name="StartsOn">The first day a delivery is expected. Left out, it is today in the household's time zone.</param>
internal sealed record AddRecipientRequest(string? Name, int ExpectedMl, DateOnly? StartsOn);

/// <summary>Request body for <c>PUT /api/v1/recipients/{id}</c>: every editable field.</summary>
internal sealed record UpdateRecipientRequest(string? Name, int ExpectedMl, DateOnly StartsOn);

/// <summary>Optional request body for <c>POST /api/v1/recipients/{id}/archive</c>.</summary>
/// <param name="ArchivedFrom">The first day without deliveries. Left out, it is today in the household's time zone.</param>
internal sealed record ArchiveRecipientRequest(DateOnly? ArchivedFrom);
