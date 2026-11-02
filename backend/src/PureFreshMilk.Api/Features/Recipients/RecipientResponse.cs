using PureFreshMilk.Application.Recipients;

namespace PureFreshMilk.Api.Features.Recipients;

/// <summary>A recipient in API responses.</summary>
/// <param name="ExpectedMl">The usual daily quantity, in whole millilitres.</param>
/// <param name="StartsOn">The first day a delivery is expected (<c>YYYY-MM-DD</c>).</param>
/// <param name="ArchivedFrom">The first day without deliveries, or <c>null</c> while the recipient is not archived.</param>
internal sealed record RecipientResponse(Guid Id, string Name, int ExpectedMl, DateOnly StartsOn, DateOnly? ArchivedFrom)
{
    public static RecipientResponse From(RecipientDetails recipient) =>
        new(recipient.Id, recipient.Name, recipient.ExpectedMl, recipient.StartsOn, recipient.ArchivedFrom);
}
