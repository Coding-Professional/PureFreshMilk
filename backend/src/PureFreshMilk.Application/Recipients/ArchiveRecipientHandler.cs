using System.Globalization;
using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Deliveries;
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
    IDeliveryRepository deliveries,
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

        var latest = await deliveries.LastDateAsync(household.Id, recipient.Id, cancellationToken);
        var from = command.ArchivedFrom ?? DefaultArchiveDate(recipient, household.Today(now), latest);
        if (latest is { } lastDate && from <= lastDate)
        {
            throw new ConflictException(ErrorCodes.RecipientNotActive,
                "The archive date must be after the latest recorded delivery.");
        }

        recipient.Archive(from, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RecipientDetails.From(recipient);
    }

    private static DateOnly DefaultArchiveDate(Recipient recipient, DateOnly today, DateOnly? latest)
    {
        // Not past the last representable day: the domain then rejects the date with its own message.
        var dayAfterStart = recipient.StartsOn == DateOnly.MaxValue ? recipient.StartsOn : recipient.StartsOn.AddDays(1);

        var afterLatest = latest is { } date && date < DateOnly.MaxValue ? date.AddDays(1) : today;
        return new[] { dayAfterStart, afterLatest, today }.Max();
    }
}
