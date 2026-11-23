using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Application.Recipients;

namespace PureFreshMilk.Application.Deliveries;

public sealed class ClearDeliveryHandler(ICurrentHousehold currentHousehold, IRecipientRepository recipients,
    IDeliveryRepository deliveries, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task HandleAsync(Guid recipientId, string requestedDate, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var date = DeliveryRules.ResolveDate(requestedDate, household, timeProvider.GetUtcNow());
        var recipient = await RecipientRules.RequireAsync(recipients, household.Id, recipientId, cancellationToken);
        var delivery = await deliveries.FindAsync(household.Id, recipient.Id, date, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        deliveries.Remove(delivery);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
