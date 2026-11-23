using PureFreshMilk.Api.Hosting;
using PureFreshMilk.Application.Deliveries;
using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Api.Features.Deliveries;

internal static class DaySheetEndpoints
{
    public static IEndpointRouteBuilder MapDaySheetEndpoints(this IEndpointRouteBuilder api)
    {
        var sheets = api.MapGroup("/day-sheets").WithTags("Day sheets");
        sheets.MapGet("/{date}", async (string date, GetDaySheetHandler handler, CancellationToken cancellationToken) =>
            TypedResults.Ok(DaySheetResponse.From(await handler.HandleAsync(date, cancellationToken)))).WithName("GetDaySheet");
        sheets.MapPost("/{date}/deliver-remaining", async (string date, DeliverRemainingHandler handler,
                CancellationToken cancellationToken) =>
            TypedResults.Ok(DaySheetResponse.From(await handler.HandleAsync(date, cancellationToken)))).WithName("DeliverRemaining");
        api.MapGet("/day-summaries", async (string? from, string? to, ListDaySummariesHandler handler,
                CancellationToken cancellationToken) =>
            {
                if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var first) ||
                    !DateOnly.TryParseExact(to, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var last))
                {
                    throw new DomainException("Both from and to must be dates in YYYY-MM-DD format.", "from");
                }

                var summaries = await handler.HandleAsync(first, last, cancellationToken);
                return TypedResults.Ok(new ItemsResponse<DaySummaryResponse>(
                    [.. summaries.Select(DaySummaryResponse.From)]));
            }).WithName("ListDaySummaries").WithTags("Day sheets");
        return api;
    }
}
