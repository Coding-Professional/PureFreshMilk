using PureFreshMilk.Api.Hosting;
using PureFreshMilk.Application.Recipients;

namespace PureFreshMilk.Api.Features.Recipients;

internal static class RecipientEndpoints
{
    public static IEndpointRouteBuilder MapRecipientEndpoints(this IEndpointRouteBuilder api)
    {
        var recipients = api.MapGroup("/recipients").WithTags("Recipients");

        recipients.MapGet(
                string.Empty,
                async (bool? includeArchived, ListRecipientsHandler handler, CancellationToken cancellationToken) =>
                {
                    var found = await handler.HandleAsync(includeArchived ?? false, cancellationToken);

                    return TypedResults.Ok(new ItemsResponse<RecipientResponse>([.. found.Select(RecipientResponse.From)]));
                })
            .WithName("ListRecipients")
            .WithSummary("Lists the recipients by name. Archived ones are left out unless includeArchived is true.");

        recipients.MapPost(
                string.Empty,
                async (AddRecipientRequest request, AddRecipientHandler handler, CancellationToken cancellationToken) =>
                {
                    var added = await handler.HandleAsync(
                        new AddRecipientCommand(request.Name, request.ExpectedMl, request.StartsOn), cancellationToken);

                    return TypedResults.Created($"{ApiRoutes.Prefix}/recipients/{added.Id}", RecipientResponse.From(added));
                })
            .WithName("AddRecipient")
            .WithSummary("Adds a recipient.");

        recipients.MapGet(
                "{id:guid}",
                async (Guid id, GetRecipientHandler handler, CancellationToken cancellationToken) =>
                    TypedResults.Ok(RecipientResponse.From(await handler.HandleAsync(id, cancellationToken))))
            .WithName("GetRecipient")
            .WithSummary("Reads one recipient.");

        recipients.MapPut(
                "{id:guid}",
                async (
                    Guid id, UpdateRecipientRequest request, UpdateRecipientHandler handler, CancellationToken cancellationToken) =>
                {
                    var updated = await handler.HandleAsync(
                        new UpdateRecipientCommand(id, request.Name, request.ExpectedMl, request.StartsOn), cancellationToken);

                    return TypedResults.Ok(RecipientResponse.From(updated));
                })
            .WithName("UpdateRecipient")
            .WithSummary("Replaces the name, usual quantity and start date of a recipient.");

        recipients.MapPost(
                "{id:guid}/archive",
                async (
                    Guid id, ArchiveRecipientRequest? request, ArchiveRecipientHandler handler, CancellationToken cancellationToken) =>
                {
                    var archived = await handler.HandleAsync(
                        new ArchiveRecipientCommand(id, request?.ArchivedFrom), cancellationToken);

                    return TypedResults.Ok(RecipientResponse.From(archived));
                })
            .WithName("ArchiveRecipient")
            .WithSummary("Stops deliveries to a recipient from a date. Their history stays.");

        recipients.MapPost(
                "{id:guid}/unarchive",
                async (Guid id, UnarchiveRecipientHandler handler, CancellationToken cancellationToken) =>
                    TypedResults.Ok(RecipientResponse.From(await handler.HandleAsync(id, cancellationToken))))
            .WithName("UnarchiveRecipient")
            .WithSummary("Brings an archived recipient back.");

        return api;
    }
}
