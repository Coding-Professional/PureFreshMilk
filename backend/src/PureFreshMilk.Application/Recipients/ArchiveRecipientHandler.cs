using System.Globalization;
using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Application.Recipients;

/// <param name="ArchivedFrom">
/// The first day without deliveries. Left out, it is today in the household's time zone, or the day after the
/// recipient starts if that is later (a recipient is active for at least one day, so one who starts today
/// cannot be archived from today).
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

        recipient.Archive(command.ArchivedFrom ?? DefaultArchiveDate(recipient, household.Today(now)), now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RecipientDetails.From(recipient);
    }

    private static DateOnly DefaultArchiveDate(Recipient recipient, DateOnly today)
    {
        // Not past the last representable day: the domain then rejects the date with its own message.
        var dayAfterStart = recipient.StartsOn == DateOnly.MaxValue ? recipient.StartsOn : recipient.StartsOn.AddDays(1);

        return dayAfterStart > today ? dayAfterStart : today;
    }
}
