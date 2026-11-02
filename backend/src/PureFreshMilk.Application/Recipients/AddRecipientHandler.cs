using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Application.Recipients;

/// <param name="ExpectedMl">The usual daily quantity, in whole millilitres.</param>
/// <param name="StartsOn">The first day a delivery is expected. Left out, it is today in the household's time zone.</param>
public sealed record AddRecipientCommand(string? Name, int ExpectedMl, DateOnly? StartsOn);

public sealed class AddRecipientHandler(
    ICurrentHousehold currentHousehold,
    IRecipientRepository recipients,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RecipientDetails> HandleAsync(AddRecipientCommand command, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        var recipient = Recipient.Add(
            household.Id,
            command.Name,
            RecipientRules.ToQuantity(command.ExpectedMl),
            command.StartsOn ?? household.Today(now),
            now);

        await RecipientRules.EnsureNameIsFreeAsync(
            recipients, household.Id, recipient.Name, exceptRecipient: null, cancellationToken);

        recipients.Add(recipient);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RecipientDetails.From(recipient);
    }
}
