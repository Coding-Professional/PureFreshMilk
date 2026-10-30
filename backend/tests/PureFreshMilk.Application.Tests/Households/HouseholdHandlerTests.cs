using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Application.Tests.Households;

public sealed class HouseholdHandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Reading_before_the_household_is_set_up_is_not_found()
    {
        var app = new TestHousehold();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => app.GetHousehold.HandleAsync(Ct));

        Assert.Equal(ErrorCodes.HouseholdNotSetUp, exception.Code);
    }

    [Fact]
    public async Task Set_up_creates_the_household_with_the_default_time_zone()
    {
        var app = new TestHousehold();

        var result = await app.SetUpHousehold.HandleAsync(new SetUpHouseholdCommand("  Example household ", null), Ct);

        Assert.True(result.Created);
        Assert.Equal("Example household", result.Household.Name);
        Assert.Equal("Asia/Kolkata", result.Household.TimeZoneId);
        Assert.Equal(TestHousehold.Today, result.Household.Today);
        Assert.Single(app.Database.Households);
        Assert.Equal(1, app.Database.SaveCount);
    }

    [Fact]
    public async Task Set_up_can_choose_the_time_zone()
    {
        var app = new TestHousehold();

        var result = await app.SetUpHousehold.HandleAsync(new SetUpHouseholdCommand("Home", "Europe/London"), Ct);

        Assert.Equal("Europe/London", result.Household.TimeZoneId);
    }

    [Fact]
    [Trait("Invariant", "H-5")]
    public async Task Setting_up_again_updates_the_only_household_instead_of_creating_a_second()
    {
        var app = await TestHousehold.SetUpAsync();
        var originalId = Assert.Single(app.Database.Households).Id.Value;

        var renamed = await app.SetUpHousehold.HandleAsync(new SetUpHouseholdCommand("Renamed", null), Ct);
        var moved = await app.SetUpHousehold.HandleAsync(new SetUpHouseholdCommand("Renamed", "Europe/London"), Ct);

        Assert.False(renamed.Created);
        Assert.False(moved.Created);
        Assert.Equal(originalId, Assert.Single(app.Database.Households).Id.Value);
        Assert.Equal("Renamed", renamed.Household.Name);
        Assert.Equal("Asia/Kolkata", renamed.Household.TimeZoneId); // an omitted time zone keeps the current one
        Assert.Equal("Europe/London", moved.Household.TimeZoneId);
    }

    [Theory]
    [InlineData(null, "Asia/Kolkata", "name")]
    [InlineData("   ", "Asia/Kolkata", "name")]
    [InlineData("Home", "Mars/Olympus_Mons", "timeZoneId")]
    [InlineData("Home", "India Standard Time", "timeZoneId")]
    public async Task Invalid_input_is_rejected_and_nothing_is_stored(string? name, string timeZoneId, string field)
    {
        var app = new TestHousehold();

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => app.SetUpHousehold.HandleAsync(new SetUpHouseholdCommand(name, timeZoneId), Ct));

        Assert.Equal(field, exception.Field);
        Assert.Empty(app.Database.Households);
        Assert.Equal(0, app.Database.SaveCount);
    }

    [Fact]
    public async Task An_invalid_update_leaves_the_household_as_it_was()
    {
        var app = await TestHousehold.SetUpAsync();

        await Assert.ThrowsAsync<DomainException>(
            () => app.SetUpHousehold.HandleAsync(new SetUpHouseholdCommand("New name", "Mars/Olympus_Mons"), Ct));

        var household = await app.GetHousehold.HandleAsync(Ct);
        Assert.Equal("Example household", household.Name);
        Assert.Equal(0, app.Database.SaveCount);
    }

    [Fact]
    public async Task The_household_reports_today_in_its_own_time_zone()
    {
        var app = await TestHousehold.SetUpAsync();

        app.Clock.Set(new DateTimeOffset(2026, 10, 30, 18, 29, 0, TimeSpan.Zero));
        Assert.Equal(new DateOnly(2026, 10, 30), (await app.GetHousehold.HandleAsync(Ct)).Today);

        app.Clock.Set(new DateTimeOffset(2026, 10, 30, 18, 30, 0, TimeSpan.Zero));
        Assert.Equal(new DateOnly(2026, 10, 31), (await app.GetHousehold.HandleAsync(Ct)).Today);
    }
}
