using Microsoft.EntityFrameworkCore;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;
using PureFreshMilk.Infrastructure.Persistence;
using PureFreshMilk.Infrastructure.Persistence.Repositories;

namespace PureFreshMilk.Infrastructure.Tests;

[Trait("Category", "Database")]
public sealed class RepositoryRoundTripTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly DateOnly October1 = new(2026, 10, 1);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Household> SaveHouseholdAsync()
    {
        await using var context = database.CreateContext();
        var household = Seed.NewHousehold();
        new HouseholdRepository(context).Add(household);
        await new UnitOfWork(context).SaveChangesAsync(Ct);
        return household;
    }

    private async Task SaveRecipientAsync(Recipient recipient)
    {
        await using var context = database.CreateContext();
        new RecipientRepository(context).Add(recipient);
        await new UnitOfWork(context).SaveChangesAsync(Ct);
    }

    [Fact]
    [Trait("Invariant", "P-2")]
    public async Task A_household_round_trips()
    {
        var household = await SaveHouseholdAsync();

        await using var context = database.CreateContext();
        var stored = await context.Households.SingleAsync(candidate => candidate.Id == household.Id, Ct);

        Assert.Equal(household.Id, stored.Id);
        Assert.Equal("Example household", stored.Name);
        Assert.Equal("Asia/Kolkata", stored.TimeZoneId);
        Assert.Equal(Seed.Now, stored.CreatedAt);
        Assert.Equal(Seed.Now, stored.UpdatedAt);
    }

    [Fact]
    [Trait("Invariant", "P-2")]
    public async Task A_recipient_round_trips_with_its_dates_and_quantity_unshifted()
    {
        var household = await SaveHouseholdAsync();
        var recipient = Seed.NewRecipient(household, "Recipient A", 1250, new DateOnly(2026, 12, 31));
        recipient.Archive(new DateOnly(2027, 1, 1), Seed.Now.AddHours(1));
        await SaveRecipientAsync(recipient);

        await using var context = database.CreateContext();
        var stored = await new RecipientRepository(context).FindAsync(household.Id, recipient.Id, Ct);

        Assert.NotNull(stored);
        Assert.Equal(recipient.Id, stored.Id);
        Assert.Equal(household.Id, stored.HouseholdId);
        Assert.Equal("Recipient A", stored.Name);
        Assert.Equal(1250, stored.ExpectedQuantity.Millilitres);
        Assert.Equal(new DateOnly(2026, 12, 31), stored.StartsOn);
        Assert.Equal(new DateOnly(2027, 1, 1), stored.ArchivedFrom);
        Assert.Equal(Seed.Now, stored.CreatedAt);
        Assert.Equal(Seed.Now.AddHours(1), stored.UpdatedAt);
    }

    [Fact]
    [Trait("Invariant", "P-2")]
    public async Task A_recipient_who_is_not_archived_has_no_archive_date()
    {
        var household = await SaveHouseholdAsync();
        var recipient = Seed.NewRecipient(household);
        await SaveRecipientAsync(recipient);

        await using var context = database.CreateContext();
        var stored = await new RecipientRepository(context).FindAsync(household.Id, recipient.Id, Ct);

        Assert.Null(Assert.IsType<Recipient>(stored).ArchivedFrom);
    }

    [Fact]
    [Trait("Invariant", "P-2")]
    public async Task A_milk_rate_round_trips()
    {
        var household = await SaveHouseholdAsync();
        var rate = Seed.NewRate(household, new DateOnly(2026, 10, 15), 6250);
        await using (var context = database.CreateContext())
        {
            new MilkRateRepository(context).Add(rate);
            await new UnitOfWork(context).SaveChangesAsync(Ct);
        }

        await using var reading = database.CreateContext();
        var stored = await new MilkRateRepository(reading).FindAsync(household.Id, rate.Id, Ct);

        Assert.NotNull(stored);
        Assert.Equal(rate.Id, stored.Id);
        Assert.Equal(household.Id, stored.HouseholdId);
        Assert.Equal(new DateOnly(2026, 10, 15), stored.EffectiveFrom);
        Assert.Equal(6250, stored.Price.PaisePerLitre);
    }

    [Fact]
    public async Task Changes_to_a_loaded_recipient_are_saved()
    {
        var household = await SaveHouseholdAsync();
        var recipient = Seed.NewRecipient(household, "Recipient A");
        await SaveRecipientAsync(recipient);

        await using (var context = database.CreateContext())
        {
            var loaded = await new RecipientRepository(context).FindAsync(household.Id, recipient.Id, Ct);
            Assert.NotNull(loaded);
            loaded.Update("Recipient Z", PureFreshMilk.Domain.Common.Quantity.FromMillilitres(500), October1, Seed.Now.AddHours(2));
            loaded.Archive(new DateOnly(2026, 11, 1), Seed.Now.AddHours(2));
            await new UnitOfWork(context).SaveChangesAsync(Ct);
        }

        await using var reading = database.CreateContext();
        var stored = await new RecipientRepository(reading).FindAsync(household.Id, recipient.Id, Ct);
        Assert.NotNull(stored);
        Assert.Equal("Recipient Z", stored.Name);
        Assert.Equal(500, stored.ExpectedQuantity.Millilitres);
        Assert.Equal(new DateOnly(2026, 11, 1), stored.ArchivedFrom);
    }

    [Fact]
    [Trait("Invariant", "R-9")]
    public async Task The_list_is_ordered_by_name_and_hides_archived_recipients_as_of_today()
    {
        var household = await SaveHouseholdAsync();
        var today = new DateOnly(2026, 10, 30);
        var active = Seed.NewRecipient(household, "bob");
        var first = Seed.NewRecipient(household, "Alice");
        var archived = Seed.NewRecipient(household, "Carol");
        archived.Archive(new DateOnly(2026, 10, 20), Seed.Now);
        var archivingLater = Seed.NewRecipient(household, "Dave");
        archivingLater.Archive(new DateOnly(2026, 12, 1), Seed.Now);
        foreach (var recipient in new[] { active, first, archived, archivingLater })
        {
            await SaveRecipientAsync(recipient);
        }

        await using var context = database.CreateContext();
        var repository = new RecipientRepository(context);
        var visible = await repository.ListAsync(household.Id, includeArchived: false, today, Ct);
        var all = await repository.ListAsync(household.Id, includeArchived: true, today, Ct);

        Assert.Equal(["Alice", "bob", "Dave"], visible.Select(recipient => recipient.Name));
        Assert.Equal(["Alice", "bob", "Carol", "Dave"], all.Select(recipient => recipient.Name));
    }

    [Fact]
    public async Task The_list_only_contains_the_recipients_of_the_requested_household()
    {
        var mine = await SaveHouseholdAsync();
        var other = await SaveHouseholdAsync();
        await SaveRecipientAsync(Seed.NewRecipient(mine, "Mine"));
        await SaveRecipientAsync(Seed.NewRecipient(other, "Theirs"));

        await using var context = database.CreateContext();
        var list = await new RecipientRepository(context).ListAsync(mine.Id, true, new DateOnly(2026, 10, 30), Ct);

        Assert.Equal("Mine", Assert.Single(list).Name);
    }

    [Fact]
    public async Task A_recipient_of_another_household_cannot_be_found()
    {
        var mine = await SaveHouseholdAsync();
        var other = await SaveHouseholdAsync();
        var theirs = Seed.NewRecipient(other, "Theirs");
        await SaveRecipientAsync(theirs);

        await using var context = database.CreateContext();

        Assert.Null(await new RecipientRepository(context).FindAsync(mine.Id, theirs.Id, Ct));
    }

    [Fact]
    [Trait("Invariant", "R-8")]
    public async Task The_name_check_ignores_case_archived_recipients_and_the_recipient_itself()
    {
        var household = await SaveHouseholdAsync();
        var active = Seed.NewRecipient(household, "Recipient A");
        var archived = Seed.NewRecipient(household, "Recipient B");
        archived.Archive(new DateOnly(2026, 10, 20), Seed.Now);
        await SaveRecipientAsync(active);
        await SaveRecipientAsync(archived);

        await using var context = database.CreateContext();
        var repository = new RecipientRepository(context);

        Assert.True(await repository.NameInUseByActiveRecipientAsync(household.Id, "recipient a", null, Ct));
        Assert.False(await repository.NameInUseByActiveRecipientAsync(household.Id, "recipient a", active.Id, Ct));
        Assert.False(await repository.NameInUseByActiveRecipientAsync(household.Id, "recipient b", null, Ct));
        Assert.False(await repository.NameInUseByActiveRecipientAsync(household.Id, "recipient c", null, Ct));
    }

    [Fact]
    public async Task The_rate_repository_lists_finds_checks_dates_and_removes()
    {
        var household = await SaveHouseholdAsync();
        var october = Seed.NewRate(household, October1, 6000);
        var mid = Seed.NewRate(household, new DateOnly(2026, 10, 15), 6400);
        await using (var context = database.CreateContext())
        {
            var repository = new MilkRateRepository(context);
            repository.Add(october);
            repository.Add(mid);
            await new UnitOfWork(context).SaveChangesAsync(Ct);
        }

        await using (var context = database.CreateContext())
        {
            var repository = new MilkRateRepository(context);

            Assert.Equal(2, (await repository.ListAsync(household.Id, Ct)).Count);
            Assert.True(await repository.EffectiveFromTakenAsync(household.Id, October1, null, Ct));
            Assert.False(await repository.EffectiveFromTakenAsync(household.Id, October1, october.Id, Ct));
            Assert.False(await repository.EffectiveFromTakenAsync(household.Id, new DateOnly(2026, 10, 2), null, Ct));

            var loaded = await repository.FindAsync(household.Id, mid.Id, Ct);
            Assert.NotNull(loaded);
            repository.Remove(loaded);
            await new UnitOfWork(context).SaveChangesAsync(Ct);
        }

        await using var reading = database.CreateContext();
        Assert.Equal(october.Id, Assert.Single(await new MilkRateRepository(reading).ListAsync(household.Id, Ct)).Id);
    }
}
