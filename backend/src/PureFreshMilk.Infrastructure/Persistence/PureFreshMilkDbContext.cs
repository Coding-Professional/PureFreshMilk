using Microsoft.EntityFrameworkCore;
using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the PostgreSQL database. Internal so that only this
/// assembly (repositories, configurations, migrations) touches persistence directly.
/// Each table is mapped by a configuration class under <c>Configurations/</c>;
/// see docs/architecture/overview.md for the mapping conventions.
/// </summary>
internal sealed class PureFreshMilkDbContext(DbContextOptions<PureFreshMilkDbContext> options)
    : DbContext(options)
{
    public DbSet<Household> Households => Set<Household>();

    public DbSet<Recipient> Recipients => Set<Recipient>();

    public DbSet<MilkRate> MilkRates => Set<MilkRate>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<HouseholdId>().HaveConversion<HouseholdIdConverter>();
        configurationBuilder.Properties<RecipientId>().HaveConversion<RecipientIdConverter>();
        configurationBuilder.Properties<MilkRateId>().HaveConversion<MilkRateIdConverter>();
        configurationBuilder.Properties<Quantity>().HaveConversion<QuantityConverter>();
        configurationBuilder.Properties<PricePerLitre>().HaveConversion<PricePerLitreConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PureFreshMilkDbContext).Assembly);
}
