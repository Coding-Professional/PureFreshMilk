using Microsoft.EntityFrameworkCore;
using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Rates;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Infrastructure.Households;
using PureFreshMilk.Infrastructure.Persistence;
using PureFreshMilk.Infrastructure.Persistence.Repositories;
using PureFreshMilk.TestSupport;

namespace PureFreshMilk.Infrastructure.Tests;

/// <summary>
/// The use cases wired to the real repositories and PostgreSQL, the way the API runs them. Single-household
/// mode needs the database to hold exactly one household, so each test starts from empty tables.
/// </summary>
[Trait("Category", "Database")]
public sealed class HandlerFlowTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly DateOnly October1 = new(2026, 10, 1);

    private readonly FixedTimeProvider clock = new(Seed.Now);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>One request's worth of services over a fresh context, as dependency injection scopes them.</summary>
    private sealed class Request(PureFreshMilkDbContext context, TimeProvider clock) : IAsyncDisposable
    {
        private readonly HouseholdRepository households = new(context);
        private readonly RecipientRepository recipients = new(context);
        private readonly MilkRateRepository rates = new(context);
        private readonly SingleHouseholdProvider current = new(context);
        private readonly UnitOfWork unitOfWork = new(context);

        public SetUpHouseholdHandler SetUp => new(current, households, unitOfWork, clock);

        public GetHouseholdHandler GetHousehold => new(current, clock);

        public AddRecipientHandler AddRecipient => new(current, recipients, unitOfWork, clock);

        public ListRecipientsHandler ListRecipients => new(current, recipients, clock);

        public UpdateRecipientHandler UpdateRecipient => new(current, recipients, unitOfWork, clock);

        public ArchiveRecipientHandler ArchiveRecipient => new(current, recipients, unitOfWork, clock);

        public UnarchiveRecipientHandler UnarchiveRecipient => new(current, recipients, unitOfWork, clock);

        public AddMilkRateHandler AddRate => new(current, rates, unitOfWork, clock);

        public ListMilkRatesHandler ListRates => new(current, rates);

        public GetRateInEffectHandler RateInEffect => new(current, rates, clock);

        public UpdateMilkRateHandler UpdateRate => new(current, rates, unitOfWork, clock);

        public DeleteMilkRateHandler DeleteRate => new(current, rates, unitOfWork);

        public ValueTask DisposeAsync() => context.DisposeAsync();
    }

    private Request NewRequest() => new(database.CreateContext(), clock);

    private async Task StartEmptyAsync()
    {
        await using var context = database.CreateContext();
        await context.Database.ExecuteSqlRawAsync("truncate table milk_rates, recipients, households", Ct);
    }

    private async Task SetUpHouseholdAsync()
    {
        await StartEmptyAsync();
        await using var request = NewRequest();
        await request.SetUp.HandleAsync(new SetUpHouseholdCommand("Example household", null), Ct);
    }

    [Fact]
    public async Task Before_set_up_the_household_is_not_found()
    {
        await StartEmptyAsync();
        await using var request = NewRequest();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => request.GetHousehold.HandleAsync(Ct));

        Assert.Equal(ErrorCodes.HouseholdNotSetUp, exception.Code);
    }

    [Fact]
    [Trait("Invariant", "H-5")]
    public async Task Setting_up_creates_one_household_and_setting_up_again_updates_it()
    {
        await StartEmptyAsync();

        await using (var request = NewRequest())
        {
            var created = await request.SetUp.HandleAsync(new SetUpHouseholdCommand("Example household", null), Ct);
            Assert.True(created.Created);
            Assert.Equal("Asia/Kolkata", created.Household.TimeZoneId);
        }

        await using (var request = NewRequest())
        {
            var updated = await request.SetUp.HandleAsync(new SetUpHouseholdCommand("Renamed", "Europe/London"), Ct);
            Assert.False(updated.Created);
        }

        await using var reading = NewRequest();
        var household = await reading.GetHousehold.HandleAsync(Ct);
        Assert.Equal("Renamed", household.Name);
        Assert.Equal("Europe/London", household.TimeZoneId);

        await using var context = database.CreateContext();
        Assert.Equal(1, await context.Households.CountAsync(Ct));
    }

    [Fact]
    [Trait("Invariant", "H-5")]
    public async Task A_second_household_row_is_noticed_instead_of_silently_ignored()
    {
        await SetUpHouseholdAsync();
        await using (var context = database.CreateContext())
        {
            context.Households.Add(Seed.NewHousehold("Intruder"));
            await context.SaveChangesAsync(Ct);
        }

        await using var request = NewRequest();

        await Assert.ThrowsAsync<InvalidOperationException>(() => request.GetHousehold.HandleAsync(Ct));
    }

    [Fact]
    public async Task Recipients_can_be_added_listed_updated_archived_and_restored()
    {
        await SetUpHouseholdAsync();

        RecipientDetails recipientA;
        await using (var request = NewRequest())
        {
            recipientA = await request.AddRecipient.HandleAsync(new AddRecipientCommand("  Recipient A ", 1000, October1), Ct);
        }

        await using (var request = NewRequest())
        {
            await request.AddRecipient.HandleAsync(new AddRecipientCommand("Recipient B", 500, null), Ct);
        }

        await using (var request = NewRequest())
        {
            var list = await request.ListRecipients.HandleAsync(includeArchived: false, Ct);
            Assert.Equal(["Recipient A", "Recipient B"], list.Select(recipient => recipient.Name));
            Assert.Equal(new DateOnly(2026, 10, 30), list[1].StartsOn); // defaulted to today in Asia/Kolkata
        }

        await using (var request = NewRequest())
        {
            var updated = await request.UpdateRecipient.HandleAsync(
                new UpdateRecipientCommand(recipientA.Id, "Recipient Z", 750, October1), Ct);
            Assert.Equal(new RecipientDetails(recipientA.Id, "Recipient Z", 750, October1, null), updated);
        }

        await using (var request = NewRequest())
        {
            var archived = await request.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(recipientA.Id, null), Ct);
            Assert.Equal(new DateOnly(2026, 10, 30), archived.ArchivedFrom);
        }

        await using (var request = NewRequest())
        {
            Assert.Equal(["Recipient B"], (await request.ListRecipients.HandleAsync(false, Ct)).Select(recipient => recipient.Name));
            Assert.Equal(2, (await request.ListRecipients.HandleAsync(true, Ct)).Count);
        }

        await using (var request = NewRequest())
        {
            var restored = await request.UnarchiveRecipient.HandleAsync(recipientA.Id, Ct);
            Assert.Null(restored.ArchivedFrom);
        }

        await using var reading = NewRequest();
        Assert.Equal(2, (await reading.ListRecipients.HandleAsync(false, Ct)).Count);
    }

    [Fact]
    [Trait("Invariant", "R-8")]
    public async Task A_duplicate_active_name_is_a_conflict_through_the_real_database_query()
    {
        await SetUpHouseholdAsync();
        await using (var request = NewRequest())
        {
            await request.AddRecipient.HandleAsync(new AddRecipientCommand("Recipient A", 1000, October1), Ct);
        }

        await using var duplicate = NewRequest();
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => duplicate.AddRecipient.HandleAsync(new AddRecipientCommand(" RECIPIENT a", 500, October1), Ct));

        Assert.Equal(ErrorCodes.RecipientNameTaken, exception.Code);
    }

    [Fact]
    public async Task Archiving_leaves_the_recipient_in_the_database()
    {
        await SetUpHouseholdAsync();
        await using (var request = NewRequest())
        {
            var added = await request.AddRecipient.HandleAsync(new AddRecipientCommand("Recipient A", 1000, October1), Ct);
            await using var archiving = NewRequest();
            await archiving.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(added.Id, null), Ct);
        }

        await using var context = database.CreateContext();
        Assert.Equal(1, await context.Recipients.CountAsync(Ct));
    }

    [Fact]
    public async Task Milk_rates_can_be_added_listed_looked_up_changed_and_deleted()
    {
        await SetUpHouseholdAsync();

        MilkRateDetails october;
        MilkRateDetails mid;
        await using (var request = NewRequest())
        {
            october = await request.AddRate.HandleAsync(new AddMilkRateCommand(October1, 6000), Ct);
        }

        await using (var request = NewRequest())
        {
            mid = await request.AddRate.HandleAsync(new AddMilkRateCommand(new DateOnly(2026, 10, 15), 6400), Ct);
        }

        await using (var request = NewRequest())
        {
            var list = await request.ListRates.HandleAsync(Ct);
            Assert.Equal([mid.Id, october.Id], list.Select(rate => rate.Id));
        }

        await using (var request = NewRequest())
        {
            Assert.Equal(6000, (await request.RateInEffect.HandleAsync(new DateOnly(2026, 10, 14), Ct)).PricePerLitrePaise);
            Assert.Equal(6400, (await request.RateInEffect.HandleAsync(null, Ct)).PricePerLitrePaise);
            await Assert.ThrowsAsync<NotFoundException>(() => request.RateInEffect.HandleAsync(new DateOnly(2026, 9, 30), Ct));
        }

        await using (var request = NewRequest())
        {
            var updated = await request.UpdateRate.HandleAsync(new UpdateMilkRateCommand(mid.Id, new DateOnly(2026, 10, 16), 6500), Ct);
            Assert.Equal(new MilkRateDetails(mid.Id, new DateOnly(2026, 10, 16), 6500), updated);
        }

        await using (var request = NewRequest())
        {
            await request.DeleteRate.HandleAsync(mid.Id, Ct);
        }

        await using var reading = NewRequest();
        Assert.Equal(october.Id, Assert.Single(await reading.ListRates.HandleAsync(Ct)).Id);
    }

    [Fact]
    [Trait("Invariant", "T-3")]
    public async Task A_duplicate_rate_date_is_a_conflict_through_the_real_database_query()
    {
        await SetUpHouseholdAsync();
        await using (var request = NewRequest())
        {
            await request.AddRate.HandleAsync(new AddMilkRateCommand(October1, 6000), Ct);
        }

        await using var duplicate = NewRequest();
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => duplicate.AddRate.HandleAsync(new AddMilkRateCommand(October1, 6400), Ct));

        Assert.Equal(ErrorCodes.RateDateTaken, exception.Code);
    }
}
