using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Rates;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Application.Status;

namespace PureFreshMilk.Application;

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

        services.AddScoped<GetHouseholdHandler>();
        services.AddScoped<SetUpHouseholdHandler>();

        services.AddScoped<ListRecipientsHandler>();
        services.AddScoped<GetRecipientHandler>();
        services.AddScoped<AddRecipientHandler>();
        services.AddScoped<UpdateRecipientHandler>();
        services.AddScoped<ArchiveRecipientHandler>();
        services.AddScoped<UnarchiveRecipientHandler>();

        services.AddScoped<ListMilkRatesHandler>();
        services.AddScoped<GetRateInEffectHandler>();
        services.AddScoped<AddMilkRateHandler>();
        services.AddScoped<UpdateMilkRateHandler>();
        services.AddScoped<DeleteMilkRateHandler>();

        return services;
    }
}
