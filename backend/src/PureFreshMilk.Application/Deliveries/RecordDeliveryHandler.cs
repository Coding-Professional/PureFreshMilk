using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Domain.Deliveries;

namespace PureFreshMilk.Application.Deliveries;

public sealed record RecordDeliveryCommand(Guid RecipientId, string Date, string? Status, int? DeliveredMl);
public sealed record RecordDeliveryResult(Delivery Delivery, bool Created);

public sealed class RecordDeliveryHandler(ICurrentHousehold currentHousehold, IRecipientRepository recipients,
    IDeliveryRepository deliveries, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<RecordDeliveryResult> HandleAsync(RecordDeliveryCommand command, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var date = DeliveryRules.ResolveDate(command.Date, household, now);
        var recipient = await RecipientRules.RequireAsync(recipients, household.Id, command.RecipientId, cancellationToken);
        if (!recipient.IsActiveOn(date))
        {
            throw new ConflictException(ErrorCodes.RecipientNotActive, "The recipient is not active on this date.");
        }

        var status = DeliveryRules.ParseStatus(command.Status);
        var existing = await deliveries.FindAsync(household.Id, recipient.Id, date, cancellationToken);
        var created = existing is null;
        if (existing is null)
        {
            existing = Delivery.Record(recipient, date, status, command.DeliveredMl, now);
            deliveries.Add(existing);
        }
        else
        {
            existing.Replace(status, command.DeliveredMl, recipient.ExpectedQuantity, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RecordDeliveryResult(existing, created);
    }
}
