using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Infrastructure.Tests;

/// <summary>Builds domain objects for the database tests. Timestamps use whole seconds: PostgreSQL keeps microseconds.</summary>
internal static class Seed
{
    public static readonly DateTimeOffset Now = new(2026, 10, 30, 6, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Start = new(2026, 10, 1);

    public static Household NewHousehold(string name = "Example household") =>
        Household.Create(name, "Asia/Kolkata", Now);

    public static Recipient NewRecipient(
        Household household, string name = "Recipient A", int millilitres = 1000, DateOnly? startsOn = null) =>
        Recipient.Add(household.Id, name, Quantity.FromMillilitres(millilitres), startsOn ?? Start, Now);

    public static MilkRate NewRate(Household household, DateOnly effectiveFrom, long paise = 6250) =>
        MilkRate.Create(household.Id, effectiveFrom, PricePerLitre.FromPaise(paise), Now);
}
