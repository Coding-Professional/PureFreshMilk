using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Application.Recipients;

/// <param name="ExpectedMl">The usual daily quantity, in whole millilitres.</param>
/// <param name="StartsOn">The first day a delivery is expected.</param>
/// <param name="ArchivedFrom">The first day without deliveries, or <c>null</c> while the recipient is not archived.</param>
public sealed record RecipientDetails(Guid Id, string Name, int ExpectedMl, DateOnly StartsOn, DateOnly? ArchivedFrom)
{
    public static RecipientDetails From(Recipient recipient) =>
        new(
            recipient.Id.Value,
            recipient.Name,
            recipient.ExpectedQuantity.Millilitres,
            recipient.StartsOn,
            recipient.ArchivedFrom);
}
