namespace MilkManager.Api.Hosting;

internal static class FrontendCors
{
    public const string PolicyName = "Frontend";

    /// <summary>
    /// Allows the web app's origins (configured under <c>Cors:AllowedOrigins</c>) to call the API.
    /// No origins are allowed unless configured.
    /// </summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}
