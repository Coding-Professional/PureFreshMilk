namespace PureFreshMilk.Domain.Common;

/// <summary>
/// Shared rules for the typed ids: UUIDv7 values (ordered by time, so inserts stay index-friendly and
/// ids cannot be guessed) that are never empty.
/// </summary>
internal static class Identifier
{
    public static Guid Create(DateTimeOffset now) => Guid.CreateVersion7(now);

    public static Guid Require(Guid value, string what) =>
        value == Guid.Empty ? throw new DomainException($"{what} cannot be empty.") : value;
}
