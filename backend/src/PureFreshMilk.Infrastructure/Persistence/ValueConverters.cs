using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Rates;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Infrastructure.Persistence;

// Domain value objects are stored as the plain value they wrap, so the tables hold uuid, integer and bigint
// columns. Registered for every property of these types in PureFreshMilkDbContext.ConfigureConventions.

internal sealed class HouseholdIdConverter()
    : ValueConverter<HouseholdId, Guid>(id => id.Value, value => HouseholdId.From(value));

internal sealed class RecipientIdConverter()
    : ValueConverter<RecipientId, Guid>(id => id.Value, value => RecipientId.From(value));

internal sealed class MilkRateIdConverter()
    : ValueConverter<MilkRateId, Guid>(id => id.Value, value => MilkRateId.From(value));

internal sealed class QuantityConverter()
    : ValueConverter<Quantity, int>(quantity => quantity.Millilitres, value => Quantity.FromMillilitres(value));

internal sealed class PricePerLitreConverter()
    : ValueConverter<PricePerLitre, long>(price => price.PaisePerLitre, value => PricePerLitre.FromPaise(value));
