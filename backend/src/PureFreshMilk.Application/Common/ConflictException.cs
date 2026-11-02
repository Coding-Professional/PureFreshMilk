namespace PureFreshMilk.Application.Common;

/// <summary>Raised when a request clashes with data that is already stored. The Api answers 409.</summary>
public sealed class ConflictException(string code, string message) : Exception(message)
{
    /// <summary>One of <see cref="ErrorCodes"/>.</summary>
    public string Code { get; } = code;
}
