using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MilkManager.Application.Status;

namespace MilkManager.Application;

public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the application use cases. Each use case is a plain handler class;
    /// there is no mediator library.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<GetSystemStatusHandler>();

        return services;
    }
}
