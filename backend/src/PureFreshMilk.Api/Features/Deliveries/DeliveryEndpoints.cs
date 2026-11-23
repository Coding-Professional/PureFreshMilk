using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using PureFreshMilk.Api.Hosting;
using PureFreshMilk.Application.Deliveries;
using PureFreshMilk.Domain.Deliveries;

namespace PureFreshMilk.Api.Features.Deliveries;

internal sealed record RecordDeliveryRequest(string? Status, int? DeliveredMl);
internal sealed record DeliveryResponse(Guid Id, Guid RecipientId, DateOnly Date, string Status, int DeliveredMl)
{
    public static DeliveryResponse From(Delivery delivery) => new(delivery.Id.Value, delivery.RecipientId.Value,
        delivery.DeliveryDate, delivery.Status == DeliveryStatus.Delivered ? "delivered" : "missed",
        delivery.DeliveredQuantity.Millilitres);
}

internal static class DeliveryEndpoints
{
    public static IEndpointRouteBuilder MapDeliveryEndpoints(this IEndpointRouteBuilder api)
    {
        var deliveries = api.MapGroup("/recipients/{recipientId:guid}/deliveries").WithTags("Deliveries");
        deliveries.MapPut("/{date}", async Task<Results<Created<DeliveryResponse>, Ok<DeliveryResponse>>> (
                Guid recipientId, string date, RecordDeliveryRequest request, RecordDeliveryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new RecordDeliveryCommand(recipientId, date,
                    request.Status, request.DeliveredMl), cancellationToken);
                var response = DeliveryResponse.From(result.Delivery);
                return result.Created
                    ? TypedResults.Created($"{ApiRoutes.Prefix}/recipients/{recipientId}/deliveries/{result.Delivery.DeliveryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}", response)
                    : TypedResults.Ok(response);
            }).WithName("RecordDelivery");
        deliveries.MapDelete("/{date}", async (Guid recipientId, string date, ClearDeliveryHandler handler,
                CancellationToken cancellationToken) =>
            {
                await handler.HandleAsync(recipientId, date, cancellationToken);
                return TypedResults.NoContent();
            }).WithName("ClearDelivery");
        return api;
    }
}
