using Microsoft.AspNetCore.Http.HttpResults;
using PureFreshMilk.Api.Hosting;
using PureFreshMilk.Application.Households;

namespace PureFreshMilk.Api.Features.Households;

internal static class HouseholdEndpoints
{
    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder api)
    {
        var household = api.MapGroup("/household").WithTags("Household");

        household.MapGet(
                string.Empty,
                async (GetHouseholdHandler handler, CancellationToken cancellationToken) =>
                    TypedResults.Ok(HouseholdResponse.From(await handler.HandleAsync(cancellationToken))))
            .WithName("GetHousehold")
            .WithSummary("Reads the household, including today's date in its time zone.");

        household.MapPut(
                string.Empty,
                async Task<Results<Created<HouseholdResponse>, Ok<HouseholdResponse>>> (
                    SetUpHouseholdRequest request, SetUpHouseholdHandler handler, CancellationToken cancellationToken) =>
                {
                    var result = await handler.HandleAsync(
                        new SetUpHouseholdCommand(request.Name, request.TimeZoneId), cancellationToken);
                    var body = HouseholdResponse.From(result.Household);

                    return result.Created
                        ? TypedResults.Created($"{ApiRoutes.Prefix}/household", body)
                        : TypedResults.Ok(body);
                })
            .WithName("SetUpHousehold")
            .WithSummary("Creates the household the first time and updates it afterwards.");

        return api;
    }
}
