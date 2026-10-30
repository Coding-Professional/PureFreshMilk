using System.Globalization;
using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.Domain.Tests.Households;

public sealed class HouseholdTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_stores_the_details_and_timestamps()
    {
        var household = Household.Create("  The Example Household ", "Asia/Kolkata", Now);

        Assert.NotEqual(Guid.Empty, household.Id.Value);
        Assert.Equal("The Example Household", household.Name);
        Assert.Equal("Asia/Kolkata", household.TimeZoneId);
        Assert.Equal(Now, household.CreatedAt);
        Assert.Equal(Now, household.UpdatedAt);
    }

    [Theory]
    [Trait("Invariant", "H-1")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_missing_name(string? name)
    {
        var exception = Assert.Throws<DomainException>(() => Household.Create(name, "Asia/Kolkata", Now));

        Assert.Equal("name", exception.Field);
    }

    [Fact]
    [Trait("Invariant", "H-1")]
    public void Name_can_have_80_characters_but_not_81()
    {
        Assert.Equal(80, Household.Create(new string('a', 80), "Asia/Kolkata", Now).Name.Length);

        var exception = Assert.Throws<DomainException>(() => Household.Create(new string('a', 81), "Asia/Kolkata", Now));
        Assert.Equal("name", exception.Field);
    }

    [Fact]
    [Trait("Invariant", "H-1")]
    public void Name_rejects_control_characters()
    {
        Assert.Throws<DomainException>(() => Household.Create("Line one\nLine two", "Asia/Kolkata", Now));
    }

    [Theory]
    [Trait("Invariant", "H-2")]
    [InlineData("Asia/Kolkata")]
    [InlineData("Europe/London")]
    [InlineData("America/Argentina/Buenos_Aires")]
    [InlineData("UTC")]
    public void Create_accepts_valid_iana_time_zones(string timeZoneId)
    {
        Assert.Equal(timeZoneId, Household.Create("Home", timeZoneId, Now).TimeZoneId);
    }

    [Theory]
    [Trait("Invariant", "H-2")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mars/Olympus_Mons")]
    [InlineData("India Standard Time")]
    [InlineData("UTC-11")]
    [InlineData("Asia/")]
    [InlineData("../etc/passwd")]
    public void Create_rejects_unknown_or_non_iana_time_zones(string? timeZoneId)
    {
        var exception = Assert.Throws<DomainException>(() => Household.Create("Home", timeZoneId, Now));

        Assert.Equal("timeZoneId", exception.Field);
    }

    [Fact]
    [Trait("Invariant", "H-2")]
    public void Time_zone_ids_longer_than_64_characters_are_rejected()
    {
        Assert.Throws<DomainException>(() => Household.Create("Home", "Asia/" + new string('a', 70), Now));
    }

    [Theory]
    [Trait("Invariant", "H-3")]
    [InlineData("2026-10-30T18:29:00Z", "2026-10-30")]
    [InlineData("2026-10-30T18:30:00Z", "2026-10-31")]
    [InlineData("2026-10-30T00:00:00Z", "2026-10-30")]
    public void Today_follows_the_household_time_zone(string utcNow, string expectedDate)
    {
        var household = Household.Create("Home", "Asia/Kolkata", Now);

        var today = household.Today(DateTimeOffset.Parse(utcNow, CultureInfo.InvariantCulture));

        Assert.Equal(DateOnly.Parse(expectedDate, CultureInfo.InvariantCulture), today);
    }

    [Theory]
    [Trait("Invariant", "H-3")]
    [InlineData("2026-10-30T03:59:00Z", "2026-10-29")]
    [InlineData("2026-10-30T04:00:00Z", "2026-10-30")]
    public void Today_accounts_for_daylight_saving_time(string utcNow, string expectedDate)
    {
        // New York is still on daylight saving time (UTC-4) on 30 October 2026.
        var household = Household.Create("Home", "America/New_York", Now);

        var today = household.Today(DateTimeOffset.Parse(utcNow, CultureInfo.InvariantCulture));

        Assert.Equal(DateOnly.Parse(expectedDate, CultureInfo.InvariantCulture), today);
    }

    [Fact]
    [Trait("Invariant", "H-4")]
    public void Asia_Kolkata_resolves_on_this_platform()
    {
        // Fails loudly where time zone data is missing, for example in a slim container image.
        var zone = TimeZoneInfo.FindSystemTimeZoneById(Household.DefaultTimeZoneId);

        Assert.Equal(TimeSpan.FromHours(5.5), zone.BaseUtcOffset);
    }

    [Fact]
    public void Update_changes_the_details_and_the_timestamp()
    {
        var household = Household.Create("Home", "Asia/Kolkata", Now);
        var later = Now.AddHours(1);

        household.Update("New name", "Europe/London", later);

        Assert.Equal("New name", household.Name);
        Assert.Equal("Europe/London", household.TimeZoneId);
        Assert.Equal(Now, household.CreatedAt);
        Assert.Equal(later, household.UpdatedAt);
    }

    [Fact]
    public void A_rejected_update_leaves_the_household_unchanged()
    {
        var household = Household.Create("Home", "Asia/Kolkata", Now);

        Assert.Throws<DomainException>(() => household.Update("New name", "Mars/Olympus_Mons", Now.AddHours(1)));

        Assert.Equal("Home", household.Name);
        Assert.Equal("Asia/Kolkata", household.TimeZoneId);
        Assert.Equal(Now, household.UpdatedAt);
    }
}
