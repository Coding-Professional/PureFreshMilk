using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;

namespace PureFreshMilk.Application.Recipients;

/// <summary>Brings an archived recipient back. Does nothing, and succeeds, if they are not archived.</summary>
public sealed class UnarchiveRecipientHandler(
    ICurrentHousehold currentHousehold,
    IRecipientRepository recipients,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RecipientDetails> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var recipient = await RecipientRules.RequireAsync(recipients, household.Id, id, cancellationToken);

        if (recipient.ArchivedFrom is not null)
        {
            // Someone else may have taken the name while this recipient was archived.
            await RecipientRules.EnsureNameIsFreeAsync(
                recipients, household.Id, recipient.Name, recipient.Id, cancellationToken);

            recipient.Unarchive(timeProvider.GetUtcNow());
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return RecipientDetails.From(recipient);
    }
}
