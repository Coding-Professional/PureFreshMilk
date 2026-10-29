using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MilkManager.Infrastructure.Persistence;

namespace MilkManager.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Configuration key: <c>ConnectionStrings:MilkManager</c>.</summary>
    public const string ConnectionStringName = "MilkManager";

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

        services.AddDbContext<MilkManagerDbContext>(options => options.UseNpgsql(connectionString));

        services.AddHealthChecks()
            .AddDbContextCheck<MilkManagerDbContext>(name: "database", tags: [HealthCheckTags.Ready]);

        return services;
    }
}
