using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Persistence;

namespace PureFreshMilk.Application.Rates;

/// <summary>
/// Removes a rate. Once billing exists, a rate that governs a finalised month will be refused here
/// (<c>period-finalised</c>).
/// </summary>
public sealed class DeleteMilkRateHandler(
    ICurrentHousehold currentHousehold, IMilkRateRepository rates, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);
        var rate = await MilkRateRules.RequireAsync(rates, household.Id, id, cancellationToken);

        rates.Remove(rate);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
