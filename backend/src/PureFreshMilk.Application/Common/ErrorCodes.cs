namespace PureFreshMilk.Application.Common;

/// <summary>
/// Stable, machine-readable codes carried by <see cref="NotFoundException"/> and
/// <see cref="ConflictException"/>. Clients branch on these, so they do not change once released.
/// </summary>
public static class ErrorCodes
{
    public const string HouseholdNotSetUp = "household-not-set-up";
    public const string RecipientNotFound = "recipient-not-found";
    public const string RecipientNameTaken = "recipient-name-taken";
    public const string RecipientAlreadyArchived = "recipient-already-archived";
    public const string RateNotFound = "rate-not-found";
    public const string RateDateTaken = "rate-date-taken";
    public const string NoRateInEffect = "no-rate-in-effect";
}
