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
}
