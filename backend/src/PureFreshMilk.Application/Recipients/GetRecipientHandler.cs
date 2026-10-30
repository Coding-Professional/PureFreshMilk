using PureFreshMilk.Application.Households;

namespace PureFreshMilk.Application.Recipients;

public sealed class GetRecipientHandler(ICurrentHousehold currentHousehold, IRecipientRepository recipients)
{
    public async Task<RecipientDetails> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);

        return RecipientDetails.From(await RecipientRules.RequireAsync(recipients, household.Id, id, cancellationToken));
    }
}
