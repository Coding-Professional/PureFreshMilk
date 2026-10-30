using System.Globalization;
using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Domain.Rates;

/// <summary>
/// A price for one litre of milk, in whole paise (ADR 0004). The upper bound keeps every amount
/// computed from it far inside the range of a <see cref="long"/>.
/// </summary>
public readonly record struct PricePerLitre : IComparable<PricePerLitre>
{
    public const long MinPaise = 1;
    public const long MaxPaise = 1_000_000;

    private PricePerLitre(long paisePerLitre) => PaisePerLitre = paisePerLitre;

    public long PaisePerLitre { get; }

    public static PricePerLitre FromPaise(long paisePerLitre)
    {
        if (paisePerLitre is < MinPaise or > MaxPaise)
        {
            throw new DomainException(
                $"A price must be between {MinPaise} and {MaxPaise} paise a litre (got {paisePerLitre.ToString(CultureInfo.InvariantCulture)}).",
                "pricePerLitrePaise");
        }

        return new PricePerLitre(paisePerLitre);
    }

    /// <summary>
    /// The cost of <paramref name="quantity"/> at this price: <c>millilitres × paise per litre ÷ 1000</c>,
    /// rounded half-up to a whole paisa. Round once per bill line, never per delivery, so pass the
    /// line's total quantity (ADR 0004).
    /// </summary>
    public Money CostOf(Quantity quantity)
    {
        var thousandthsOfPaisa = checked((long)quantity.Millilitres * PaisePerLitre);
        const long halfOfDivisor = Quantity.MillilitresPerLitre / 2;

        return Money.FromPaise(checked(thousandthsOfPaisa + halfOfDivisor) / Quantity.MillilitresPerLitre);
    }

    public int CompareTo(PricePerLitre other) => PaisePerLitre.CompareTo(other.PaisePerLitre);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{PaisePerLitre} paise a litre");
}
