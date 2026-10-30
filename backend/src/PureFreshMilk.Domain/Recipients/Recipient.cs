using System.Globalization;
using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.Domain.Recipients;

/// <summary>
/// Someone in the household who receives a daily milk delivery. Never deleted: a recipient who stops
/// receiving milk is archived, so their history stays (ADR 0004).
/// </summary>
public sealed class Recipient
{
    public const int MinExpectedMillilitres = 1;
    public const int MaxExpectedMillilitres = 20_000;

    // Persistence constructor.
    private Recipient()
    {
    }

    public RecipientId Id { get; private set; }

    public HouseholdId HouseholdId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>The usual daily quantity. Applied when a delivery is recorded without one.</summary>
    public Quantity ExpectedQuantity { get; private set; }

    /// <summary>The first day a delivery is expected.</summary>
    public DateOnly StartsOn { get; private set; }

    /// <summary>The first day on which no delivery is expected, or <c>null</c> while the recipient is not archived.</summary>
    public DateOnly? ArchivedFrom { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Recipient Add(
        HouseholdId householdId, string? name, Quantity expectedQuantity, DateOnly startsOn, DateTimeOffset now)
    {
        var recipient = new Recipient
        {
            Id = RecipientId.New(now),
            HouseholdId = householdId,
            CreatedAt = now,
        };
        recipient.Apply(name, expectedQuantity, startsOn, now);
        return recipient;
    }

    public void Update(string? name, Quantity expectedQuantity, DateOnly startsOn, DateTimeOffset now) =>
        Apply(name, expectedQuantity, startsOn, now);

    /// <summary>
    /// The name as it will be stored (trimmed), or a rule violation if it is not a valid name. Lets a caller
    /// compare names, for example to look for a duplicate, before changing anything.
    /// </summary>
    public static string NormalizeName(string? name) => NameRules.Require(name, "name", "The recipient's name");

    /// <summary>Stops deliveries from <paramref name="from"/> onwards. The days before it keep their history.</summary>
    public void Archive(DateOnly from, DateTimeOffset now)
    {
        if (ArchivedFrom is not null)
        {
            throw new DomainException("The recipient is already archived.");
        }

        if (from <= StartsOn)
        {
            throw new DomainException(
                $"A recipient must be active for at least one day, so the archive date must be after the start date ({FormatDate(StartsOn)}).",
                "archivedFrom");
        }

        ArchivedFrom = from;
        UpdatedAt = now;
    }

    /// <summary>Brings an archived recipient back. Does nothing if the recipient is not archived.</summary>
    public void Unarchive(DateTimeOffset now)
    {
        if (ArchivedFrom is null)
        {
            return;
        }

        ArchivedFrom = null;
        UpdatedAt = now;
    }

    /// <summary>Whether a delivery is expected on <paramref name="date"/>: from the start date up to the day before archiving.</summary>
    public bool IsActiveOn(DateOnly date) =>
        date >= StartsOn && (ArchivedFrom is null || date < ArchivedFrom);

    /// <summary>Whether the recipient has been archived as of <paramref name="today"/> (an archive date in the future still counts as active).</summary>
    public bool IsArchivedOn(DateOnly today) => ArchivedFrom is { } from && from <= today;

    private void Apply(string? name, Quantity expectedQuantity, DateOnly startsOn, DateTimeOffset now)
    {
        // Check everything before changing anything, so a rejected update leaves the recipient as it was.
        var validName = NormalizeName(name);

        if (expectedQuantity.Millilitres is < MinExpectedMillilitres or > MaxExpectedMillilitres)
        {
            throw new DomainException(
                $"The usual quantity must be between {MinExpectedMillilitres} ml and {MaxExpectedMillilitres} ml (got {expectedQuantity.Millilitres} ml).",
                "expectedMl");
        }

        if (startsOn == default)
        {
            throw new DomainException("The start date is required.", "startsOn");
        }

        if (ArchivedFrom is { } archivedFrom && startsOn >= archivedFrom)
        {
            throw new DomainException(
                $"The start date must be before the archive date ({FormatDate(archivedFrom)}).", "startsOn");
        }

        Name = validName;
        ExpectedQuantity = expectedQuantity;
        StartsOn = startsOn;
        UpdatedAt = now;
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
