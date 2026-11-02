using System.Globalization;

namespace PureFreshMilk.Domain.Common;

/// <summary>
/// An amount of Indian rupees held as a signed whole number of paise, so that sums are exact and never
/// suffer from floating-point rounding (see ADR 0004). INR is the only currency. Unlike
/// <see cref="Quantity"/> it may be negative, because adjustments can be.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    private Money(long paise) => Paise = paise;

    public long Paise { get; }

    public static Money Zero => default;

    public static Money FromPaise(long paise) => new(paise);

    public static Money operator +(Money left, Money right) =>
        new(checked(left.Paise + right.Paise));

    public static Money operator -(Money left, Money right) =>
        new(checked(left.Paise - right.Paise));

    public static Money operator -(Money value) => new(checked(-value.Paise));

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public int CompareTo(Money other) => Paise.CompareTo(other.Paise);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Paise} paise");
}
