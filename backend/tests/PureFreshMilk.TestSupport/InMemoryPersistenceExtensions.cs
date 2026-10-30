using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Application.Rates;
using PureFreshMilk.Application.Recipients;

namespace PureFreshMilk.TestSupport;

public static class InMemoryPersistenceExtensions
{
    /// <summary>
    /// Replaces the real repositories, unit of work and current household with in-memory ones, so the
    /// HTTP pipeline can be tested without a database. Returns the database so a test can inspect it.
    /// </summary>
    public static InMemoryDatabase UseInMemoryPersistence(this IServiceCollection services)
    {
        var database = new InMemoryDatabase();
        var households = new InMemoryHouseholdRepository(database);

        services.RemoveAll<IHouseholdRepository>();
        services.RemoveAll<ICurrentHousehold>();
        services.RemoveAll<IRecipientRepository>();
        services.RemoveAll<IMilkRateRepository>();
        services.RemoveAll<IUnitOfWork>();

        services.AddSingleton(database);
        services.AddSingleton<IUnitOfWork>(database);
        services.AddSingleton<IHouseholdRepository>(households);
        services.AddSingleton<ICurrentHousehold>(households);
        services.AddSingleton<IRecipientRepository>(new InMemoryRecipientRepository(database));
        services.AddSingleton<IMilkRateRepository>(new InMemoryMilkRateRepository(database));

        return database;
    }
}
