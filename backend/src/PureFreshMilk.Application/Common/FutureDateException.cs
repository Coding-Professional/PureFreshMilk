using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Application.Common;

/// <summary>A request refers to a household date after today.</summary>
public sealed class FutureDateException(string field)
    : DomainException("A future date cannot be recorded or requested.", field);
