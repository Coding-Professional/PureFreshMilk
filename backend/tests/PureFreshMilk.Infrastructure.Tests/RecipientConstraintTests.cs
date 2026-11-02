using Microsoft.EntityFrameworkCore;
using Npgsql;
using PureFreshMilk.Application.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Infrastructure.Persistence;

namespace PureFreshMilk.Infrastructure.Tests;

[Trait("Category", "Database")]
public sealed class RecipientConstraintTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<Household> SaveHouseholdAsync()
    {
        await using var context = database.CreateContext();
        var household = Seed.NewHousehold();
        context.Households.Add(household);
        await context.SaveChangesAsync(Ct);
        return household;
    }

    private async Task SaveAsync(Action<PureFreshMilkDbContext> change)
    {
        await using var context = database.CreateContext();
        change(context);
        await new UnitOfWork(context).SaveChangesAsync(Ct);
    }

    [Fact]
    [Trait("Invariant", "R-8")]
    public async Task Active_names_are_unique_within_a_household_ignoring_case()
    {
        var household = await SaveHouseholdAsync();
        await SaveAsync(context => context.Recipients.Add(Seed.NewRecipient(household, "Recipient A")));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => SaveAsync(context => context.Recipients.Add(Seed.NewRecipient(household, "RECIPIENT a"))));

        Assert.Equal(ErrorCodes.RecipientNameTaken, exception.Code);
    }

    [Fact]
    [Trait("Invariant", "R-8")]
    public async Task The_same_name_can_be_used_in_another_household()
    {
        var first = await SaveHouseholdAsync();
        var second = await SaveHouseholdAsync();

        await SaveAsync(context => context.Recipients.Add(Seed.NewRecipient(first, "Recipient A")));
        await SaveAsync(context => context.Recipients.Add(Seed.NewRecipient(second, "Recipient A")));
    }

    [Fact]
    [Trait("Invariant", "R-8")]
    public async Task An_archived_recipient_does_not_block_its_name()
    {
        var household = await SaveHouseholdAsync();
        var archived = Seed.NewRecipient(household, "Recipient A");
        archived.Archive(new DateOnly(2026, 10, 15), Seed.Now);
        await SaveAsync(context => context.Recipients.Add(archived));

        await SaveAsync(context => context.Recipients.Add(Seed.NewRecipient(household, "Recipient A")));
    }

    [Fact]
    [Trait("Invariant", "R-8")]
    public async Task Unarchiving_into_a_name_that_is_now_taken_is_refused_by_the_index()
    {
        var household = await SaveHouseholdAsync();
        var archived = Seed.NewRecipient(household, "Recipient A");
        archived.Archive(new DateOnly(2026, 10, 15), Seed.Now);
        await SaveAsync(context => context.Recipients.Add(archived));
        await SaveAsync(context => context.Recipients.Add(Seed.NewRecipient(household, "Recipient A")));

        var exception = await Assert.ThrowsAsync<ConflictException>(() => SaveAsync(context =>
        {
            var stored = context.Recipients.Single(recipient => recipient.Id == archived.Id);
            stored.Unarchive(Seed.Now);
        }));

        Assert.Equal(ErrorCodes.RecipientNameTaken, exception.Code);
    }

    [Fact]
    public async Task The_name_key_is_generated_from_the_name()
    {
        var household = await SaveHouseholdAsync();
        var recipient = Seed.NewRecipient(household, "Recipient MIXED Case");
        await SaveAsync(context => context.Recipients.Add(recipient));

        await using var context = database.CreateContext();
        var key = await context.Database
            .SqlQuery<string>($"select name_key as \"Value\" from recipients where id = {recipient.Id.Value}")
            .SingleAsync(Ct);

        Assert.Equal("recipient mixed case", key);
    }

    [Theory]
    [Trait("Invariant", "R-2")]
    [InlineData("Recipient A", 0, "2026-10-01", null, "ck_recipients_expected_ml")]
    [InlineData("Recipient A", 20_001, "2026-10-01", null, "ck_recipients_expected_ml")]
    [InlineData("   ", 500, "2026-10-01", null, "ck_recipients_name_length")]
    [InlineData("Recipient A", 500, "2026-10-01", "2026-10-01", "ck_recipients_archived_after_start")]
    [InlineData("Recipient A", 500, "2026-10-01", "2026-09-01", "ck_recipients_archived_after_start")]
    public async Task The_database_refuses_rows_that_break_the_checks_even_when_the_domain_is_bypassed(
        string name, int expectedMl, string startsOn, string? archivedFrom, string constraint)
    {
        var household = await SaveHouseholdAsync();
        await using var context = database.CreateContext();
        var start = DateOnly.Parse(startsOn, System.Globalization.CultureInfo.InvariantCulture);
        DateOnly? archived = archivedFrom is null ? null : DateOnly.Parse(archivedFrom, System.Globalization.CultureInfo.InvariantCulture);

        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"""
            insert into recipients (id, household_id, name, expected_ml, starts_on, archived_from, created_at, updated_at)
            values ({Guid.CreateVersion7()}, {household.Id.Value}, {name}, {expectedMl}, {start}, {archived}, {Seed.Now}, {Seed.Now})
            """,
            Ct));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(constraint, exception.ConstraintName);
    }

    [Fact]
    public async Task A_recipient_cannot_point_at_a_household_that_does_not_exist()
    {
        await using var context = database.CreateContext();

        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"""
            insert into recipients (id, household_id, name, expected_ml, starts_on, created_at, updated_at)
            values ({Guid.CreateVersion7()}, {Guid.CreateVersion7()}, {"Recipient A"}, {500}, {Seed.Start}, {Seed.Now}, {Seed.Now})
            """,
            Ct));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Equal("fk_recipients_households_household_id", exception.ConstraintName);
    }

    [Fact]
    public async Task A_household_with_recipients_cannot_be_deleted()
    {
        var household = await SaveHouseholdAsync();
        await SaveAsync(context => context.Recipients.Add(Seed.NewRecipient(household)));
        await using var context = database.CreateContext();

        var exception = await Assert.ThrowsAsync<PostgresException>(
            () => context.Database.ExecuteSqlAsync($"delete from households where id = {household.Id.Value}", Ct));

        // RESTRICT, rather than NO ACTION, reports its own error code.
        Assert.Equal(PostgresErrorCodes.RestrictViolation, exception.SqlState);
        Assert.Equal("fk_recipients_households_household_id", exception.ConstraintName);
    }
}
