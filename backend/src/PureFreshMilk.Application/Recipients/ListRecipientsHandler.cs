using PureFreshMilk.Application.Households;

namespace PureFreshMilk.Application.Recipients;

/// <summary>Lists the household's recipients by name. Archived recipients are left out unless asked for.</summary>
public sealed class ListRecipientsHandler(
    ICurrentHousehold currentHousehold, IRecipientRepository recipients, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<RecipientDetails>> HandleAsync(
        bool includeArchived, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var today = household.Today(timeProvider.GetUtcNow());

        var found = await recipients.ListAsync(household.Id, includeArchived, today, cancellationToken);

        return [.. found.Select(RecipientDetails.From)];
    }
}
