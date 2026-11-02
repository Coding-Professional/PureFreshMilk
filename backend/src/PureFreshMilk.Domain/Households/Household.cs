using System.Text.RegularExpressions;
using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Domain.Households;

/// <summary>
/// A household that buys milk. It owns the notion of "today": dates in this application are calendar
/// dates in the household's own time zone (ADR 0004).
/// </summary>
public sealed partial class Household
{
    public const string DefaultTimeZoneId = "Asia/Kolkata";

    // Persistence constructor.
    private Household()
    {
    }

    public HouseholdId Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>An IANA time zone id such as <c>Asia/Kolkata</c>.</summary>
    public string TimeZoneId { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Household Create(string? name, string? timeZoneId, DateTimeOffset now)
    {
        var household = new Household { Id = HouseholdId.New(now), CreatedAt = now };
        household.Apply(name, timeZoneId, now);
        return household;
    }

    public void Update(string? name, string? timeZoneId, DateTimeOffset now) => Apply(name, timeZoneId, now);

    /// <summary>The calendar date it is right now in the household's time zone.</summary>
    public DateOnly Today(DateTimeOffset utcNow)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utcNow, zone).DateTime);
    }

    private void Apply(string? name, string? timeZoneId, DateTimeOffset now)
    {
        // Check everything before changing anything, so a rejected update leaves the household as it was.
        var validName = NameRules.Require(name, "name", "The household name");
        var validTimeZoneId = RequireTimeZone(timeZoneId);

        Name = validName;
        TimeZoneId = validTimeZoneId;
        UpdatedAt = now;
    }

    private static string RequireTimeZone(string? timeZoneId)
    {
        var id = timeZoneId?.Trim() ?? string.Empty;

        // Only IANA ids are stored: Windows ids ("India Standard Time") would not resolve on Linux.
        if (!IanaIdPattern().IsMatch(id) || id.Length > 64)
        {
            throw new DomainException(
                "The time zone must be an IANA id such as Asia/Kolkata.", "timeZoneId");
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new DomainException($"The time zone '{id}' is not known.", "timeZoneId");
        }

        return id;
    }

    [GeneratedRegex(@"^[A-Za-z_]+(/[A-Za-z0-9_+\-]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex IanaIdPattern();
}
