namespace PureFreshMilk.Application.Common;

/// <summary>Raised when something the caller asked for does not exist. The Api answers 404.</summary>
public sealed class NotFoundException(string code, string message) : Exception(message)
{
    /// <summary>One of <see cref="ErrorCodes"/>.</summary>
    public string Code { get; } = code;
}
