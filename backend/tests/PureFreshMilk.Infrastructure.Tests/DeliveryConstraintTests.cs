using Microsoft.EntityFrameworkCore;
using Npgsql;
using PureFreshMilk.Domain.Deliveries;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;
using PureFreshMilk.Infrastructure.Persistence;

namespace PureFreshMilk.Infrastructure.Tests;

[Trait("Category", "Database")]
public sealed class DeliveryConstraintTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<(Household Household, Recipient Recipient)> SeedAsync()
    {
        await using var context = database.CreateContext();
        await context.Database.ExecuteSqlRawAsync("truncate table deliveries, milk_rates, recipients, households", Ct);
        var household = Seed.NewHousehold();
        var recipient = Seed.NewRecipient(household);
        context.Households.Add(household);
        context.Recipients.Add(recipient);
        await context.SaveChangesAsync(Ct);
        return (household, recipient);
    }

    [Fact]
    [Trait("Invariant", "L-7")]
    public async Task Unique_index_keeps_one_delivery_per_recipient_and_date()
    {
        var (household, recipient) = await SeedAsync();
        await using (var context = database.CreateContext())
        {
            context.Deliveries.Add(Delivery.Record(recipient, Seed.Start, DeliveryStatus.Delivered, null, Seed.Now));
            await context.SaveChangesAsync(Ct);
        }

        await using var duplicate = database.CreateContext();
        duplicate.Deliveries.Add(Delivery.Record(recipient, Seed.Start, DeliveryStatus.Missed, null, Seed.Now));
        var exception = await Assert.ThrowsAsync<PureFreshMilk.Application.Common.ConflictException>(
            () => new UnitOfWork(duplicate).SaveChangesAsync(Ct));
        Assert.Equal("delivery-date-taken", exception.Code);
        Assert.Equal(1, await duplicate.Deliveries.AsNoTracking().CountAsync(delivery => delivery.HouseholdId == household.Id, Ct));
    }

    [Fact]
    [Trait("Invariant", "L-10")]
    public async Task Simultaneous_inserts_leave_one_row_and_report_the_loser()
    {
        var (household, recipient) = await SeedAsync();

        async Task<Exception?> WriteAsync(DeliveryStatus status)
        {
            await using var context = database.CreateContext();
            context.Deliveries.Add(Delivery.Record(recipient, Seed.Start, status, null, Seed.Now));
            try
            {
                await new UnitOfWork(context).SaveChangesAsync(Ct);
                return null;
            }
            catch (PureFreshMilk.Application.Common.ConflictException exception)
            {
                return exception;
            }
        }

        var results = await Task.WhenAll(WriteAsync(DeliveryStatus.Delivered), WriteAsync(DeliveryStatus.Missed));
        Assert.Single(results, result => result is null);
        Assert.Single(results, result => result is PureFreshMilk.Application.Common.ConflictException);
        await using var reading = database.CreateContext();
        Assert.Equal(1, await reading.Deliveries.CountAsync(delivery => delivery.HouseholdId == household.Id, Ct));
    }

    [Theory]
    [Trait("Invariant", "L-12")]
    [InlineData("delivered", 0, "ck_deliveries_quantity")]
    [InlineData("missed", 500, "ck_deliveries_quantity")]
    [InlineData("unknown", 0, "ck_deliveries_quantity")]
    public async Task Database_rejects_invalid_status_and_quantity(string status, int ml, string constraint)
    {
        var (household, recipient) = await SeedAsync();
        await using var context = database.CreateContext();
        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"insert into deliveries (id, household_id, recipient_id, delivery_date, status, delivered_ml, created_at, updated_at) values ({Guid.CreateVersion7()}, {household.Id.Value}, {recipient.Id.Value}, {Seed.Start}, {status}, {ml}, {Seed.Now}, {Seed.Now})", Ct));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal(constraint, exception.ConstraintName);
    }

    [Fact]
    [Trait("Invariant", "L-9")]
    public async Task Composite_foreign_key_rejects_another_households_recipient()
    {
        var (_, recipient) = await SeedAsync();
        await using var context = database.CreateContext();
        var other = Seed.NewHousehold("Another household");
        context.Households.Add(other);
        await context.SaveChangesAsync(Ct);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"insert into deliveries (id, household_id, recipient_id, delivery_date, status, delivered_ml, created_at, updated_at) values ({Guid.CreateVersion7()}, {other.Id.Value}, {recipient.Id.Value}, {Seed.Start}, {"delivered"}, {500}, {Seed.Now}, {Seed.Now})", Ct));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, exception.SqlState);
        Assert.Equal("fk_deliveries_recipients_household_id_recipient_id", exception.ConstraintName);
    }
}
