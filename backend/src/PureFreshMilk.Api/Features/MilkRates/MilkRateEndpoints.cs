using PureFreshMilk.Api.Hosting;
using PureFreshMilk.Application.Rates;

namespace PureFreshMilk.Api.Features.MilkRates;

internal static class MilkRateEndpoints
{
    public static IEndpointRouteBuilder MapMilkRateEndpoints(this IEndpointRouteBuilder api)
    {
        var rates = api.MapGroup("/milk-rates").WithTags("Milk rates");

        rates.MapGet(
                string.Empty,
                async (ListMilkRatesHandler handler, CancellationToken cancellationToken) =>
                {
                    var found = await handler.HandleAsync(cancellationToken);

                    return TypedResults.Ok(new ItemsResponse<MilkRateResponse>([.. found.Select(MilkRateResponse.From)]));
                })
            .WithName("ListMilkRates")
            .WithSummary("Lists the rates, newest effective date first.");

        rates.MapGet(
                "in-effect",
                async (DateOnly? on, GetRateInEffectHandler handler, CancellationToken cancellationToken) =>
                    TypedResults.Ok(MilkRateResponse.From(await handler.HandleAsync(on, cancellationToken))))
            .WithName("GetMilkRateInEffect")
            .WithSummary("The rate in force on a date (default: today). 404 if no rate has started yet.");

        rates.MapPost(
                string.Empty,
                async (MilkRateRequest request, AddMilkRateHandler handler, CancellationToken cancellationToken) =>
                {
                    var added = await handler.HandleAsync(
                        new AddMilkRateCommand(request.EffectiveFrom, request.PricePerLitrePaise), cancellationToken);

                    return TypedResults.Created($"{ApiRoutes.Prefix}/milk-rates/{added.Id}", MilkRateResponse.From(added));
                })
            .WithName("AddMilkRate")
            .WithSummary("Adds a rate.");

        rates.MapPut(
                "{id:guid}",
                async (Guid id, MilkRateRequest request, UpdateMilkRateHandler handler, CancellationToken cancellationToken) =>
                {
                    var updated = await handler.HandleAsync(
                        new UpdateMilkRateCommand(id, request.EffectiveFrom, request.PricePerLitrePaise), cancellationToken);

                    return TypedResults.Ok(MilkRateResponse.From(updated));
                })
            .WithName("UpdateMilkRate")
            .WithSummary("Changes the date or price of a rate.");

        rates.MapDelete(
                "{id:guid}",
                async (Guid id, DeleteMilkRateHandler handler, CancellationToken cancellationToken) =>
                {
                    await handler.HandleAsync(id, cancellationToken);

                    return TypedResults.NoContent();
                })
            .WithName("DeleteMilkRate")
            .WithSummary("Removes a rate.");

        return api;
    }
}
