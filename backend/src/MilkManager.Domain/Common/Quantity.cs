using System.Globalization;

namespace MilkManager.Domain.Common;

/// <summary>
/// A non-negative volume of milk. Stored as whole millilitres so that daily and
/// monthly totals are exact and never suffer from floating-point rounding.
/// </summary>
public readonly record struct Quantity : IComparable<Quantity>
{
    public const int MillilitresPerLitre = 1000;

    private Quantity(int millilitres) => Millilitres = millilitres;

    public int Millilitres { get; }

    public static Quantity Zero => default;

    public static Quantity FromMillilitres(int millilitres)
    {
        if (millilitres < 0)
        {
            throw new DomainException($"A quantity cannot be negative (got {millilitres} ml).");
        }

        return new Quantity(millilitres);
    }

    public static Quantity FromLitres(decimal litres)
    {
        var millilitres = litres * MillilitresPerLitre;

        if (millilitres != decimal.Truncate(millilitres))
        {
            throw new DomainException(
                $"A quantity must be a whole number of millilitres (got {litres.ToString(CultureInfo.InvariantCulture)} L).");
        }

        if (millilitres > int.MaxValue)
        {
            throw new DomainException("A quantity is too large.");
        }

        return FromMillilitres((int)millilitres);
    }

    public static Quantity operator +(Quantity left, Quantity right) =>
        new(checked(left.Millilitres + right.Millilitres));

    public static bool operator <(Quantity left, Quantity right) => left.CompareTo(right) < 0;

    public static bool operator >(Quantity left, Quantity right) => left.CompareTo(right) > 0;

    public static bool operator <=(Quantity left, Quantity right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Quantity left, Quantity right) => left.CompareTo(right) >= 0;

    public int CompareTo(Quantity other) => Millilitres.CompareTo(other.Millilitres);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Millilitres} ml");
}
