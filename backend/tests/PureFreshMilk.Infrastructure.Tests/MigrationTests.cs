using Microsoft.EntityFrameworkCore;

namespace PureFreshMilk.Infrastructure.Tests;

[Trait("Category", "Database")]
public sealed class MigrationTests(PostgresDatabaseFixture database) : IClassFixture<PostgresDatabaseFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("Invariant", "P-1")]
    public async Task Migrations_apply_to_an_empty_database_and_the_model_has_no_further_changes()
    {
        await using var context = database.CreateContext();

        var applied = await context.Database.GetAppliedMigrationsAsync(Ct);

        Assert.Contains(applied, name => name.EndsWith("_AddHouseholdsRecipientsAndMilkRates", StringComparison.Ordinal));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync(Ct));
        Assert.False(context.Database.HasPendingModelChanges(), "The model differs from the last migration: add a migration.");
    }

    [Fact]
    public async Task The_tables_have_the_columns_and_types_that_the_design_lists()
    {
        await using var context = database.CreateContext();

        var columns = await context.Database
            .SqlQueryRaw<string>(
                """
                select table_name || '.' || column_name || ' ' || data_type || ' ' || is_nullable as "Value"
                from information_schema.columns
                where table_schema = 'public' and table_name in ('households', 'recipients', 'milk_rates')
                order by 1
                """)
            .ToListAsync(Ct);

        Assert.Equal(
            [
                "households.created_at timestamp with time zone NO",
                "households.id uuid NO",
                "households.name character varying NO",
                "households.time_zone_id character varying NO",
                "households.updated_at timestamp with time zone NO",
                "milk_rates.created_at timestamp with time zone NO",
                "milk_rates.effective_from date NO",
                "milk_rates.household_id uuid NO",
                "milk_rates.id uuid NO",
                "milk_rates.price_per_litre_paise bigint NO",
                "milk_rates.updated_at timestamp with time zone NO",
                "recipients.archived_from date YES",
                "recipients.created_at timestamp with time zone NO",
                "recipients.expected_ml integer NO",
                "recipients.household_id uuid NO",
                "recipients.id uuid NO",
                "recipients.name character varying NO",
                "recipients.name_key character varying YES",
                "recipients.starts_on date NO",
                "recipients.updated_at timestamp with time zone NO",
            ],
            columns);
    }

    [Fact]
    public async Task The_constraints_and_indexes_of_the_design_exist()
    {
        await using var context = database.CreateContext();

        var constraints = await context.Database
            .SqlQueryRaw<string>(
                """
                select conname as "Value" from pg_constraint
                where conrelid in ('households'::regclass, 'recipients'::regclass, 'milk_rates'::regclass)
                  and contype in ('p', 'u', 'f', 'c') -- PostgreSQL 18 also lists NOT NULL ('n') here
                order by conname
                """)
            .ToListAsync(Ct);

        Assert.Equal(
            [
                "ck_households_name_length",
                "ck_milk_rates_price",
                "ck_recipients_archived_after_start",
                "ck_recipients_expected_ml",
                "ck_recipients_name_length",
                "fk_milk_rates_households_household_id",
                "fk_recipients_households_household_id",
                "pk_households",
                "pk_milk_rates",
                "pk_recipients",
                "uq_recipients_household_id_id",
            ],
            constraints);
    }

    [Fact]
    public async Task Foreign_keys_restrict_deletion_so_history_outlives_any_delete()
    {
        await using var context = database.CreateContext();

        // 'r' is RESTRICT in pg_constraint.confdeltype.
        var rules = await context.Database
            .SqlQueryRaw<string>(
                """
                select conname || ' ' || confdeltype::text as "Value" from pg_constraint
                where connamespace = 'public'::regnamespace and contype = 'f'
                order by conname
                """)
            .ToListAsync(Ct);

        Assert.Equal(
            ["fk_milk_rates_households_household_id r", "fk_recipients_households_household_id r"],
            rules);
    }

    [Fact]
    public async Task Active_recipient_names_are_unique_through_a_partial_index_on_the_generated_name_key()
    {
        await using var context = database.CreateContext();

        var definitions = await context.Database
            .SqlQueryRaw<string>(
                """
                select indexdef as "Value" from pg_indexes
                where schemaname = 'public' and indexname in
                  ('ux_recipients_household_id_name_key_active', 'ux_milk_rates_household_id_effective_from')
                order by indexname
                """)
            .ToListAsync(Ct);

        Assert.Equal(2, definitions.Count);
        Assert.Contains("UNIQUE INDEX ux_milk_rates_household_id_effective_from", definitions[0], StringComparison.Ordinal);
        Assert.Contains("(household_id, effective_from)", definitions[0], StringComparison.Ordinal);
        Assert.Contains("UNIQUE INDEX ux_recipients_household_id_name_key_active", definitions[1], StringComparison.Ordinal);
        Assert.Contains("(household_id, name_key)", definitions[1], StringComparison.Ordinal);
        Assert.Contains("WHERE (archived_from IS NULL)", definitions[1], StringComparison.Ordinal);
    }
}
