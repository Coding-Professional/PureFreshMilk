namespace PureFreshMilk.Domain.Common;

/// <summary>
/// Raised when an operation would break a business rule.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, string? field)
        : base(message)
    {
        Field = field;
    }

    /// <summary>
    /// The single input the broken rule is about, named as in the API (for example <c>expectedMl</c>),
    /// or <c>null</c> when the rule concerns several inputs or none.
    /// </summary>
    public string? Field { get; }
}
