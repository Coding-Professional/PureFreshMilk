using MilkManager.Application.Status;

namespace MilkManager.Api.Features.Status;

internal static class StatusEndpoints
{
    private const string ServiceName = "milk-manager-api";

    public static IEndpointRouteBuilder MapStatusEndpoints(this IEndpointRouteBuilder api)
    {
        api.MapGet("/status", (GetSystemStatusHandler handler) =>
            {
                var status = handler.Handle();
                return TypedResults.Ok(new StatusResponse(ServiceName, status.Version, status.ServerTime));
            })
            .WithName("GetStatus")
            .WithTags("Status")
            .WithSummary("Reports the API version and the server time.");

        return api;
    }
}
