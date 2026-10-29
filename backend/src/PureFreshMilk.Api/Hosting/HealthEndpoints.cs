using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using PureFreshMilk.Infrastructure;

namespace PureFreshMilk.Api.Hosting;

internal static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Liveness: the process is running. Deliberately runs no dependency checks.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

        // Readiness: dependencies such as PostgreSQL are reachable.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Ready),
        });

        return app;
    }
}
