using System.Globalization;
using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;

namespace PureFreshMilk.Application.Recipients;

/// <param name="ArchivedFrom">
/// The first day without deliveries. Left out, it is today in the household's time zone.
/// </param>
public sealed record ArchiveRecipientCommand(Guid Id, DateOnly? ArchivedFrom);

/// <summary>Stops deliveries to a recipient from a date. Their history stays; nothing is deleted.</summary>
public sealed class ArchiveRecipientHandler(
    ICurrentHousehold currentHousehold,
    IRecipientRepository recipients,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RecipientDetails> HandleAsync(ArchiveRecipientCommand command, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var recipient = await RecipientRules.RequireAsync(recipients, household.Id, command.Id, cancellationToken);
        var now = timeProvider.GetUtcNow();

        if (recipient.ArchivedFrom is { } archivedFrom)
        {
            throw new ConflictException(
                ErrorCodes.RecipientAlreadyArchived,
                $"The recipient is already archived from {archivedFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}. Bring them back before archiving again.");
        }

        recipient.Archive(command.ArchivedFrom ?? household.Today(now), now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RecipientDetails.From(recipient);
    }
}
