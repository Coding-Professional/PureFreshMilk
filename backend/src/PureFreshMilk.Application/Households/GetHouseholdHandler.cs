namespace PureFreshMilk.Application.Households;

/// <summary>Reads the household, including today's date in its time zone.</summary>
public sealed class GetHouseholdHandler(ICurrentHousehold currentHousehold, TimeProvider timeProvider)
{
    public async Task<HouseholdDetails> HandleAsync(CancellationToken cancellationToken)
    {
        var household = await currentHousehold.RequireAsync(cancellationToken);

        return HouseholdDetails.From(household, timeProvider);
    }
}
