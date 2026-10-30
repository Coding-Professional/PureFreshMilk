using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Application.Recipients;

/// <summary>Replaces the editable fields of a recipient.</summary>
public sealed record UpdateRecipientCommand(Guid Id, string? Name, int ExpectedMl, DateOnly StartsOn);

public sealed class UpdateRecipientHandler(
    ICurrentHousehold currentHousehold,
    IRecipientRepository recipients,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RecipientDetails> HandleAsync(UpdateRecipientCommand command, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var recipient = await RecipientRules.RequireAsync(recipients, household.Id, command.Id, cancellationToken);

        // Look for a clash before changing anything, so a rejected request leaves the recipient as it was.
        var name = Recipient.NormalizeName(command.Name);
        if (recipient.ArchivedFrom is null)
        {
            await RecipientRules.EnsureNameIsFreeAsync(recipients, household.Id, name, recipient.Id, cancellationToken);
        }

        recipient.Update(name, RecipientRules.ToQuantity(command.ExpectedMl), command.StartsOn, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RecipientDetails.From(recipient);
    }
}
