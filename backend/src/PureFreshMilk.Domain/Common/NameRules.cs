namespace PureFreshMilk.Domain.Common;

/// <summary>The rule shared by every display name: trimmed, 1 to 80 characters, no control characters.</summary>
internal static class NameRules
{
    public const int MaxLength = 80;

    public static string Require(string? value, string field, string what)
    {
        var name = value?.Trim() ?? string.Empty;

        if (name.Length == 0)
        {
            throw new DomainException($"{what} is required.", field);
        }

        if (name.Length > MaxLength)
        {
            throw new DomainException($"{what} can have at most {MaxLength} characters.", field);
        }

        if (name.Any(char.IsControl))
        {
            throw new DomainException($"{what} cannot contain control characters.", field);
        }

        return name;
    }
}
