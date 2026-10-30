using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Application.Rates;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Infrastructure.Households;
using PureFreshMilk.Infrastructure.Persistence;
using PureFreshMilk.Infrastructure.Persistence.Repositories;

namespace PureFreshMilk.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Configuration key: <c>ConnectionStrings:PureFreshMilk</c>.</summary>
    public const string ConnectionStringName = "PureFreshMilk";

    /// <summary>
    /// Registers persistence and the database readiness check.
    /// Fails fast when the connection string is missing rather than failing on the first query.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. " +
                $"Set the ConnectionStrings__{ConnectionStringName} environment variable or a user secret. " +
                "See docs/development.md.");
        }

        services.AddDbContext<PureFreshMilkDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurrentHousehold, SingleHouseholdProvider>();
        services.AddScoped<IHouseholdRepository, HouseholdRepository>();
        services.AddScoped<IRecipientRepository, RecipientRepository>();
        services.AddScoped<IMilkRateRepository, MilkRateRepository>();

        // Ready means PostgreSQL answers and every migration has been applied. Without the second part the
        // API would start and then fail every request with "relation does not exist".
        services.AddHealthChecks()
            .AddDbContextCheck<PureFreshMilkDbContext>(
                name: "database",
                tags: [HealthCheckTags.Ready],
                customTestQuery: async (context, cancellationToken) =>
                    !(await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any());

        return services;
    }
}
