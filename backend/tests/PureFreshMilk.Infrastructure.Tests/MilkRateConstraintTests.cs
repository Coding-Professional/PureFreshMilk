using Microsoft.EntityFrameworkCore;
using Npgsql;
using PureFreshMilk.Application.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Infrastructure.Persistence;

namespace PureFreshMilk.Infrastructure.Tests;

[Trait("Category", "Database")]
public sealed class MilkRateConstraintTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private static readonly DateOnly October1 = new(2026, 10, 1);

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
    [Trait("Invariant", "T-3")]
    public async Task A_second_rate_for_the_same_day_is_refused_by_the_unique_index()
    {
        var household = await SaveHouseholdAsync();
        await SaveAsync(context => context.MilkRates.Add(Seed.NewRate(household, October1, 6000)));

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => SaveAsync(context => context.MilkRates.Add(Seed.NewRate(household, October1, 6400))));

        Assert.Equal(ErrorCodes.RateDateTaken, exception.Code);
    }

    [Fact]
    public async Task Two_households_can_have_a_rate_starting_on_the_same_day()
    {
        var first = await SaveHouseholdAsync();
        var second = await SaveHouseholdAsync();

        await SaveAsync(context => context.MilkRates.Add(Seed.NewRate(first, October1)));
        await SaveAsync(context => context.MilkRates.Add(Seed.NewRate(second, October1)));
    }

    [Theory]
    [Trait("Invariant", "T-1")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    public async Task The_database_refuses_a_price_outside_the_allowed_range_even_when_the_domain_is_bypassed(long paise)
    {
        var household = await SaveHouseholdAsync();
        await using var context = database.CreateContext();

        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"""
            insert into milk_rates (id, household_id, effective_from, price_per_litre_paise, created_at, updated_at)
            values ({Guid.CreateVersion7()}, {household.Id.Value}, {October1}, {paise}, {Seed.Now}, {Seed.Now})
            """,
            Ct));

        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_milk_rates_price", exception.ConstraintName);
    }

    [Fact]
    public async Task A_rate_cannot_point_at_a_household_that_does_not_exist()
    {
        await using var context = database.CreateContext();

        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"""
            insert into milk_rates (id, household_id, effective_from, price_per_litre_paise, created_at, updated_at)
            values ({Guid.CreateVersion7()}, {Guid.CreateVersion7()}, {October1}, {6250L}, {Seed.Now}, {Seed.Now})
            """,
            Ct));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Equal("fk_milk_rates_households_household_id", exception.ConstraintName);
    }
}
